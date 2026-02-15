using System.ComponentModel;
using System.Net;
using Kromer.Models.Exceptions.Attributes;

namespace Kromer.Models.Exceptions;

public enum ErrorCode
{
    [Description("The address could not be found")]
    [StatusCode(HttpStatusCode.NotFound)]
    AddressNotFound,
    
    [Description("The name could not be found")]
    [StatusCode(HttpStatusCode.NotFound)]
    NameNotFound,
    
    [Description("The name is already taken")]
    [StatusCode(HttpStatusCode.Conflict)]
    NameTaken,
    
    [Description("Insufficient funds")]
    [StatusCode(HttpStatusCode.BadRequest)]
    InsufficientFunds,
    
    [Description("Invalid request parameter")]
    [StatusCode(HttpStatusCode.BadRequest)]
    InvalidParameter,
    
    [Description("Invalid name ownership")]
    [StatusCode(HttpStatusCode.Forbidden)]
    NotNameOwner,
    
    [Description("Invalid amount number")]
    [StatusCode(HttpStatusCode.Forbidden)]
    InvalidAmount,
}