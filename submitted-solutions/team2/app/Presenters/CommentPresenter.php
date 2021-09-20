<?php

declare(strict_types = 1);

namespace App\Presenters;

use App\Model\CommentRepository;
use Nette\DI\Attributes\Inject;
use Nette\Http\IRequest;
use Nette\Http\IResponse;
use Nette\Utils\Json;
use Nette\Schema\Expect;
use Nette\Schema\Processor;
use App\Model\Helpers;
use Exception;

class CommentPresenter extends BasePresenter
{
	#[Inject] public CommentRepository $commentRepository;

	protected function startup()
	{
		parent::startup();
		//Debugger::log($this->getHttpRequest()->getMethod());
	}

	public function actionWebpage(
            string $location = '',
            int $limit = 0,
            string $after = null,
            int $replies1stLevelLimit = 0,
            int $replies2ndLevelLimit = 0,
            int $replies3rdLevelLimit = 0,
	): void
	{
            if ($this->request->isMethod(IRequest::GET)) {
                $this->sendJson($this->get(
                        $location,
                        $limit,
                        $after,
                        $replies1stLevelLimit,
                        $replies2ndLevelLimit,
                        $replies3rdLevelLimit,
                ));
            }
            if ($this->request->isMethod(IRequest::POST)) {
                $this->checkLocation($location);
                
                $commentInputJSON = $this->getHttpRequest()->getRawBody();
                $commentInput = Json::decode($commentInputJSON, Json::FORCE_ARRAY);

                if (!isset($commentInput['author']) || !isset($commentInput['text'])) {
                    $this->error('Bad args.', IResponse::S400_BAD_REQUEST);
                }
                
                $this->checkUuid(uuid: $commentInput['parent'] ?? null, name: 'parent', isAllowedToBeNull: true);
                $this->checkUuid(uuid: $commentInput['author'], name: 'author');
                $this->checkText($commentInput['text']);
                
                //coment insert 201
                $retVal = $this->commentRepository->insertComment($location, $commentInput);
                
                if (!$retVal) {
                    $this->error('Fail in db. Maybe duplicated comment?', IResponse::S400_BAD_REQUEST);
                }
            
                $this->getHttpResponse()->setCode(IResponse::S201_CREATED);
                
                $this->sendJson($retVal);
            }
            
            $this->error('Wrong method please use ' . IRequest::GET . ' or ' . IRequest::POST, IResponse::S400_BAD_REQUEST);
	}

        public function actionComment(
            string $id,
        ): void
        {
            $this->checkMethod(IRequest::GET);
            
            $this->sendJson($this->getByUuid($id));
        }

        public function actionDelete(): void
        {
            $this->checkMethod(IRequest::DELETE);
            
            $this->commentRepository->deleteAll();
            
            $this->getHttpResponse()->setCode(IResponse::S204_NO_CONTENT);
            $this->sendPayload();
        }

        public function actionBatch(): void
        {
            $this->checkMethod(IRequest::POST);
            
            $batchInputJSON = $this->getHttpRequest()->getRawBody();
            $batchInput = Json::decode($batchInputJSON, Json::FORCE_ARRAY);
            
            $schemaString = Expect::structure([
                'id' => Expect::string(),
            ])->castTo('array');
            
            $schemaStructure = Expect::structure([
                'location' => Expect::string()
                    ->required(),
                'limit' => Expect::int()
                    ->min(Helpers::PARAM_INT_MIN)
                    ->max(Helpers::INT_32_MAX)
                    ->required(),
                'after' => Expect::anyOf(
                        Expect::string(),
                        Expect::null(),
                    ),
                'replies1stLevelLimit' => Expect::int()
                    ->min(Helpers::PARAM_INT_MIN)
                    ->max(Helpers::INT_32_MAX)
                    ->default(0),
                'replies2ndLevelLimit' => Expect::int()
                    ->min(Helpers::PARAM_INT_MIN)
                    ->max(Helpers::INT_32_MAX)
                    ->default(0),
                'replies3rdLevelLimit' => Expect::int()
                    ->min(Helpers::PARAM_INT_MIN)
                    ->max(Helpers::INT_32_MAX)
                    ->default(0),
            ])->castTo('array');
            
            $globalStructure = Expect::arrayOf(
                Expect::anyOf(
                    $schemaString,
                    $schemaStructure,
                ),
            )->castTo('array');
            
            try {
                $processor = new Processor;
                $processedStructure = $processor->process($globalStructure, $batchInput);
            } catch (Exception $ex) {
                $this->error($ex->getMessage(), IResponse::S400_BAD_REQUEST);
            }
            
            $retVal = array_fill(0, count($processedStructure) - 1, null);

            try {
				foreach ($processedStructure as $key => $item) {
					if (count($item) == 1) {
						$retVal[$key] = $this->getByUuid(...$item);
					} else {
						$retVal[$key] = $this->get(...$item);
					}
				}
            } catch (Exception $e) {
				$this->sendJson([]);
			}
            
            $this->sendJson($retVal);
        }

        private function getByUuid(
            string $id,
        ): array
        {
            $this->checkUuid($id, 'id');
            
            $comment = $this->commentRepository->getCommentByUuid($id);
            
            if (!$comment) {
                $this->error('Comment not found');
            }
            
            return $comment;
        }

        private function get(
            string $location,
            int $limit,
            ?string $after,
            int $replies1stLevelLimit,
            int $replies2ndLevelLimit,
            int $replies3rdLevelLimit,
        ): array
        {
            $this->checkLocation($location);
            $this->checkParamInt($limit, 'limit');
            $this->checkParamInt($replies3rdLevelLimit, 'replies3rdLevelLimit', $replies2ndLevelLimit, false);
            $this->checkParamInt($replies2ndLevelLimit, 'replies2ndLevelLimit', $replies1stLevelLimit, false);
            $this->checkParamInt($replies1stLevelLimit, 'replies1stLevelLimit', required: false);

            [
                'uuid' => $uuid,
                'returnChild' => $returnChild,
            ] = $this->checkCursor($after);

            try {
                 $retVal = $this->commentRepository->get(
                        $location,
                        $uuid,
						(bool) $returnChild,
                        $limit,
                        $replies1stLevelLimit,
                        $replies2ndLevelLimit,
                        $replies3rdLevelLimit,
                ) ?? [
                	'pageInfo' => [
						'hasNextPage' => false,
					],
					 'edges' => [],
			 	];
            }catch (Exception $ex) {
                $this->error($ex->getMessage(), $ex->getCode());
            }

            return $retVal;
        }

}
