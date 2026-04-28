#include "FilePersistence.h"


bool FilePersistence::init( const std::string& fileName )
{
	if (fileName.empty()) {
		return false;
	}

	try {
		if (_outFile.is_open()) {
			_outFile.close();
		}

		_outFile.clear();
		
		//abrimos truncando o no dependiendo de _truncateOnInit
		const std::ios::openmode openMode = _truncateOnInit
			? (std::ios::out | std::ios::trunc | std::ios::binary)
			: (std::ios::out | std::ios::app | std::ios::binary);

		_outFile.open(fileName, openMode);
		if (!_outFile.is_open() || _outFile.fail()) {
			return false;
		}

		return true;
	}
	catch (...) {
		return false;
	}
}

bool FilePersistence::close() noexcept {
	
	try {
		if (!_outFile.is_open()) {
			return true;
		}

		_outFile.flush();
		if (_outFile.bad()) {
			_outFile.close();
			return false;
		}

		_outFile.close();
		return !_outFile.bad();
	}
	catch (...) {
		return false;
	}
}

bool FilePersistence::persist(const DataChunk& data) noexcept
{
	if (data.empty()) {
		return true;
	}

	if (!_outFile.is_open() || _outFile.fail()) {
		return false;
	}

	try {
		_outFile.write(reinterpret_cast<const char*>(data.data()), static_cast<std::streamsize>(data.size()));
		if (_outFile.fail() || _outFile.bad()) {
			return false;
		}

		_outFile.flush();
		return !_outFile.fail() && !_outFile.bad();
	}
	catch (...) {
		return false;
	}
}
