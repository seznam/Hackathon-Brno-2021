<?php

declare(strict_types = 1);

namespace App\Model;

use Exception;
use Nette\Database\Explorer;
use Nette\Database\Table\ActiveRow;
use Nette\Database\Table\Selection;
use Nette\Http\IResponse;
use Nette\Utils\Strings;
use Nette\Caching\Cache;
use Nette\Caching\Storage;

class CommentRepository extends BaseModel
{
	private Cache $cache;

	public function __construct(
		Explorer $db,
		Storage $storage,
	) {
		$this->cache = new Cache($storage, 'CommentRepository');
		parent::__construct($db);
		//$this->deleteAll();
	}

	protected function getTableName(): string
	{
		return 'comment';
	}

	public function getAll(): Selection
	{
		return parent::getAll()
			->order('append_order ASC');
	}

	public function deleteAll(): void
	{
		$this->delete($this->getAll());
	}

	public function getCommentByUuid(
		string|ActiveRow $uuid,
	): ?array
	{
		if (is_string($uuid)) {
			$node = $this->getbyUuid($uuid);
		} else {
			$node = $uuid;
		}

		if (!$node) {
			return null;
		}

		return $this->getComment(
			$node,
			'',
			0,
			0,
			0,
		);
	}

	public function get(
		string $location,
		?string $uuid,
		bool $returnChild,
		int $limit,
		int $limit1,
		int $limit2,
		int $limit3,
		ActiveRow $elem = null,
	): ?array
	{
		if ($limit == 0) {
			throw new Exception('Low limit', IResponse::S400_BAD_REQUEST);
		}

		if ($returnChild) {
			$retVal = $this->getAll()
				->where('location', $location)
				->where('comment_id', $uuid)
				->limit($limit);

			if ($elem) {
				$retVal->where('append_order > ?', $elem->append_order);
			}

			return $this->getCommentsConnection(
				$retVal,
				$location,
				$limit1,
				$limit2,
				$limit3,
			);
		}

		$node = $this->getbyUuid($uuid);

		return $this->get(
			$location,
			$node->comment_id,
			true,
			$limit,
			$limit1,
			$limit2,
			$limit3,
			$node,
		);
	}

	public function insertComment(
		string $location,
		array $commentInput,
	): ?array
	{
		$uuid = Strings::truncate(sha1(
			$commentInput['parent'] . $commentInput['author'] . $commentInput['text'] . (microtime(true) * 100000)
		), 35, '');

		if ($commentInput['parent']) {
			$parent = $this->getbyUuid($commentInput['parent']);
			if ($location != $parent->location) {
				return null;
			}
		}

		$toInsert = [
			'id' => $uuid,
			'comment_id' => $commentInput['parent'],
			'created' => floor(microtime(true) * 1000),
			'replies_start_cursor' => $uuid . '_1',
			'siblings_start_cursor' => $uuid . '_0',
			'location' => $location,
			'author' => $commentInput['author'],
			'text' => $commentInput['text'],
		];

		try {
			$count = $this->cache->load('insert_order', function () {
				return 1;
			});
			$toInsert['append_order'] = $count;
			$inserted = $this->insert($toInsert);
			$this->cache->save('insert_order', $count + 1);
		} catch (Exception $e) {
			return null;
		}

		return $this->getCommentByUuid($inserted);
	}

	private function getCommentsConnection(
		Selection $nodes,
		string $location,
		int $limit1,
		int $limit2,
		int $limit3,
	): ?array
	{
		$count = $nodes->count('id');
		if (!$count) {
			return null;
		}

		$retVal = [
			'edges' => array_fill(0, $count-1, null),
			'pageInfo' => null,
		];

		$lastNode = null;
		$i = 0;

		foreach ($nodes as $node) {
			$lastNode = $node;
			$retVal['edges'][$i++] = $this->getCommentsEdge(
				$node,
				$location,
				$limit1,
				$limit2,
				$limit3,
			);
		}

		$retVal['pageInfo'] = $this->getPageInfo($lastNode);

		return $retVal;
	}

	private function getCommentsEdge(
		ActiveRow $node,
		string $location,
		int $limit1,
		int $limit2,
		int $limit3,
	): array
	{
		return [
			'cursor' => $node->siblings_start_cursor,
			'node' => $this->getComment(
				$node,
				$location,
				$limit1,
				$limit2,
				$limit3,
			),
		];
	}

	private function getComment(
		ActiveRow $node,
		string $location,
		int $limit1,
		int $limit2,
		int $limit3,
	): array
	{
		try {
			$replies = $this->get(
				$location,
				$node->id,
				true,
				$limit1,
				$limit2,
				$limit3,
				0,
			);
		} catch (Exception $ex) {
			$replies = null;
		}
		return [
			'id' => $node->id,
			'author' => $node->author,
			'text' => $node->text,
			'parent' => $this->getNode($node->comment_id),
			'created' => $node->created,
			'repliesStartCursor' => $node->replies_start_cursor,
			'replies' => $replies,
		];
	}

	private function getNode(?string $id): ?array
	{
		if (!$id) {
			return null;
		}

		return [
			'id' => $id,
		];
	}

	private function getPageInfo(?ActiveRow $node): array
	{
		$retVal = $this->getAll()
			->where('location', $node->location)
			->where('comment_id', $node->comment_id)
			->where('append_order > ?', $node->append_order)
			->limit(1)
			->count('id');
		$endCursor = $node->siblings_start_cursor ?? '';

		return [
			'hasNextPage' => (bool) $retVal,
			'endCursor' => $endCursor,
		];
	}

}
