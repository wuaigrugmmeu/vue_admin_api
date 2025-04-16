using System;

namespace UserPermissionSystem.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException() { }
        protected DomainException(string message) : base(message) { }
        protected DomainException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class UserDomainException : DomainException
    {
        public UserDomainException() { }
        public UserDomainException(string message) : base(message) { }
        public UserDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class RoleDomainException : DomainException
    {
        public RoleDomainException() { }
        public RoleDomainException(string message) : base(message) { }
        public RoleDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class PermissionDomainException : DomainException
    {
        public PermissionDomainException() { }
        public PermissionDomainException(string message) : base(message) { }
        public PermissionDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}