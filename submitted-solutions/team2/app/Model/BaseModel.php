<?php

declare(strict_types = 1);

namespace App\Model;

use Nette\Database\Explorer;
use Nette\Database\Table\ActiveRow;
use Nette\Database\Table\Selection;
use Nette\InvalidArgumentException;
use Nette\SmartObject;

abstract class BaseModel
{
	use SmartObject;

	public function __construct(
		private Explorer $db,
	) {	}

	protected abstract function getTableName(): string;

	public function getAll(): Selection
	{
		return $this->db->table($this->getTableName());
	}

	public function getById(int $id): ?ActiveRow
	{
		return $this->getAll()
			->get($id);
	}

	public function getbyUuid(string $uuid): ?ActiveRow
	{
		return $this->getAll()
			->get($uuid);
	}

	public function insert(array $data): ActiveRow|int
	{
		return $this->getAll()->insert($data);
	}

	public function update(array $data, string|ActiveRow $id): bool
	{
		if (is_string($id)) {
			$id = $this->getbyUuid($id);
		}
		if (!$id) {
			throw new InvalidArgumentException('Not in database', 404);
		}
		return $id->update($data);
	}

	public function delete(string|ActiveRow|Selection $id): int
	{
		if (is_string($id)) {
			$id = $this->getbyUuid($id);
		}
		if (!$id) {
			throw new InvalidArgumentException('Not in database', 404);
		}
		return $id->delete();
	}

}
