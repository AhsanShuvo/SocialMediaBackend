namespace SocialMediaBackend.API.Models
{
    public class TokenResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public TokenData Data { get; set; }

        public TokenResponse(int code, string message, TokenData? data = null)
        {
            StatusCode = code;
            Message = message;
            Data = data;
        }

        public static TokenResponse Failure(int statusCode, string message) => new TokenResponse(statusCode, message);
        public static TokenResponse Success(TokenData token) => new TokenResponse(200, "success", token);
    }

    public class TokenData
    {
        public string Token { get; set; }
        public int ExpiresIn { get; set; }

        public TokenData(string token, int expiresIn)
        {
            Token = token;
            ExpiresIn = expiresIn;
        }
    }
}
