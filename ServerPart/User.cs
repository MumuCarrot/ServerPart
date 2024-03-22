﻿using Newtonsoft.Json;

/// <summary>
/// An implementation of user
/// </summary>
public class User
{
    [JsonProperty("username")]
    public string UserName { get; set; } = string.Empty;
    [JsonProperty("login")]
    public string Login { get; set; } = string.Empty;
    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
    [JsonProperty("aboutme")]
    public string AboutMe { get; set; } = string.Empty;
}