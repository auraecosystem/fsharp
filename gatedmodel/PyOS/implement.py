import abc
import typing as t


class PathLike(abc.ABC):

    """Abstract base class for implementing the file system path protocol."""

    @abc.abstractmethod
    def __fspath__(self) -> t.Union[str, bytes]:
        """Return the file system path representation of the object."""
        raise NotImplementedError
