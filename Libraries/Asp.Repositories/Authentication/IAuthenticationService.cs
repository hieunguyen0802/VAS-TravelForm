﻿namespace src.Repositories.Authentication
{
    public interface IAuthenticationService
    {
        bool ValidateUser(string domain, string userName, string password);
    }
}