namespace IdentityCore.Models;

/// <summary>
/// Represents the result of an operation.
/// </summary>
/// <typeparam name="T">The type of data returned by the operation.</typeparam>
public class OperationResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult{T}"/> class.
    /// The operation is considered successful.
    /// </summary>
    public OperationResult()
    {
        Success = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult{T}"/> class with the specified data.
    /// The operation is considered successful.
    /// </summary>
    /// <param name="data">The data returned by the operation.</param>
    public OperationResult(T data)
    {
        Data = data;
        Success = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult{T}"/> class with the specified error message.
    /// The operation is considered unsuccessful.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public OperationResult(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Success = false;
    }

    public T Data { get; set; }

    public string ErrorMessage { get; set; }
    
    public bool Success { get; set; }
}