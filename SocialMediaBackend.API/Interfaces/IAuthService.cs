﻿using SocialMediaBackend.API.Models;

namespace SocialMediaBackend.API.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponse> AuthenticateAsync(Guid userId);
    }
}
