<?php

declare(strict_types=1);

require __DIR__ . '/../vendor/autoload.php';

if (false && $_SERVER['REMOTE_ADDR'] != '127.0.0.1')
die();

App\Bootstrap::boot()
	->createContainer()
	->getByType(Nette\Application\Application::class)
	->run();
