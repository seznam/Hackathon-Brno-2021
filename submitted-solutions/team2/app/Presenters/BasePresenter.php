<?php

declare(strict_types = 1);

namespace App\Presenters;

use App\Model\Helpers;
use Nette\Application\UI\Presenter;
use Nette\Http\IResponse;
use Nette\Utils\Validators;
use Nette\Utils\Strings;
use Tracy\Debugger;

class BasePresenter extends Presenter
{
    protected function checkUuid(?string $uuid, string $name, bool $isAllowedToBeNull = false): void
    {
        if (!$uuid) {
            if (!$isAllowedToBeNull) {
                $this->error('We are sorry, but ' . $name . ' is required.', IResponse::S400_BAD_REQUEST);
            }
            return;
        }
        
        $length = Strings::length($uuid);
        
        if ($length < Helpers::MIN_ID_LENGTH || $length > Helpers::MAX_ID_LENGTH) {
                $this->error('We are sorry, but ' . $name . ' is in wrong format', IResponse::S400_BAD_REQUEST);
        }
    }
    protected function checkMethod(string $method): void
	{
		if ($this->request->isMethod($method)) {
			return;
		}

		$this->error('Wrong method please use ' . $method, IResponse::S400_BAD_REQUEST);
	}

	protected function checkLocation(
		string $location,
	): void
	{
		if (!Validators::isUrl($location)) {
			$this->error('We are sorry, location is not valid URL', IResponse::S400_BAD_REQUEST);
		}
	}

	protected function checkCursor(
		string $cursor = null,
	): array
	{
		if (!$cursor) {
			return [
				'uuid' => null,
				'returnChild' => 1,
			];
		}

		$exploded = explode('_', $cursor);
		$countExploded = count($exploded);

		if ($countExploded != 2) {
			$this->error('We are sorry, but cursor must be in format `uuid_bool` where bool is 1 if you need childs or 0 if you need siblings', IResponse::S400_BAD_REQUEST);
		}
                
		$this->checkUuid($exploded[0], 'cursor');

		if ($exploded[1] !== '1' && $exploded[1] !== '0') {
			$this->error('We are sorry, but bool is not bool', IResponse::S400_BAD_REQUEST);
		}

		return [
			'uuid' => $exploded[0],
			'returnChild' => (int)$exploded[1],
		];
	}

	protected function checkParamInt(
		int $param,
		string $paramName,
		int $dependsOn = null,
		bool $required = true,
	): void
	{
		if (!$required && $param === 0) {
			return;
		}

		if ($param > Helpers::INT_32_MAX) {
			$this->error('We are sorry, but parameter ' . $paramName . ' must be less or equal to ' . Helpers::INT_32_MAX, IResponse::S400_BAD_REQUEST);
		}

		if ($param < Helpers::PARAM_INT_MIN) {
			$this->error('We are sorry, but parameter ' . $paramName . ' must be greater or equal to ' . Helpers::PARAM_INT_MIN, IResponse::S400_BAD_REQUEST);
		}

		if ($param > 0 && $dependsOn !== null && $dependsOn < 1) {
			$this->error('We are sorry, but parameter ' . $paramName . ' has another required parameter, which must be set.', IResponse::S400_BAD_REQUEST);
		}
	}
        
        protected function checkText (
            string $text,
        ): void
        {
        	$length = Strings::length($text);

            if ($length < Helpers::MIN_TEXT_LENGTH || $length > Helpers::MAX_TEXT_LENGTH) {
                    $this->error('We are sorry, but text must be less than ' . Helpers::MAX_TEXT_LENGTH . 'and more than ' . Helpers::MIN_TEXT_LENGTH, IResponse::S400_BAD_REQUEST);
            }
        }
}
