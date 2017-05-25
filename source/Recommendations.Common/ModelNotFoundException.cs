using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Recommendations.Common
{
    /// <summary>
    /// The exception thrown by <see cref="ModelsProvider"/> when a model cannot be found in storage.
    /// </summary>
    [Serializable]
    public class ModelNotFoundException : Exception
    {
        /// <summary>
        /// Gets or sets the ID of the model that cannot be found.
        /// </summary>
        public Guid ModelId { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelNotFoundException" /> class.
        /// </summary>
        public ModelNotFoundException() : this(null) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelNotFoundException" /> class.
        /// </summary>
        /// <param name="message">The message for the exception.</param>
        /// <param name="innerException">The <see cref="T:System.Exception" /> object that is the cause of the current exception. If the <see cref="P:System.Exception.InnerException" /> parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch block that handles the inner exception.</param>
        public ModelNotFoundException(string message, Exception innerException = null)
            : this(message, Guid.Empty, innerException) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelNotFoundException" /> class.
        /// </summary>
        /// <param name="message">The message for the exception.</param>
        /// <param name="modelId">The ID of the model that cannot be found.</param>
        /// <param name="innerException">The <see cref="T:System.Exception" /> object that is the cause of the current exception. If the <see cref="P:System.Exception.InnerException" /> parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch block that handles the inner exception.</param>
        public ModelNotFoundException(string message, Guid modelId, Exception innerException = null)
            : base(message, innerException)
        {
            ModelId = modelId;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelNotFoundException" /> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> structure that contains contextual information about the source or destination.</param>
        protected ModelNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ModelId = Guid.Parse(info.GetString(nameof(ModelId)));
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo" /> object with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> structure that contains contextual information about the source or destination.</param>
        [SecurityCritical]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ModelId), ModelId.ToString(), typeof(string));
            base.GetObjectData(info, context);
        }
    }
}
