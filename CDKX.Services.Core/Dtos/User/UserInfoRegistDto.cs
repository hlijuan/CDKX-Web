﻿using System;
using CDKX.Services.Core.Models.User;
using OSharp.Core.Data;

namespace CDKX.Services.Core.Dtos.User
{
    public class UserInfoRegistDto : IAddDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string RealName { get; set; }
        public string Password { get; set; }
        public string NickName { get; set; }
        public string HeadPic { get; set; }
        public string RegistKey { get; set; }
        public string ChannelCode { get; set; }
        public Sex Sex { get; set; }
        public string Email { get; set; }
    }
}
