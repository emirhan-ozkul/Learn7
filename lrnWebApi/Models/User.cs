using System;
using System.Globalization;

namespace lrnWebApi.Models;

public class User
{
    public string Username {get;set;} = string.Empty;
    public string email {get;set;} = string.Empty;
    public string password{get;set;} = string.Empty;
}
