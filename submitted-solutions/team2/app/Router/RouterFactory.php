<?php

declare(strict_types=1);

namespace App\Router;

use Nette;
use Nette\Application\Routers\Route;
use Nette\Application\Routers\RouteList;
use Tracy\Debugger;

final class RouterFactory
{
	use Nette\StaticClass;

	public static function createRouter(): RouteList
	{
		$router = new RouteList;
		$router->addRoute('webPage/[<location .*>/]comment', 'Comment:webpage')
			->addRoute('comment', 'Comment:delete')
			->addRoute('comment/<id>', 'Comment:comment')
			->addRoute('batch', 'Comment:batch');

		return $router;
	}
}
