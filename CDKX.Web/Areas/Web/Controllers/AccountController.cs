﻿using System;
using System.Data.Entity;
using System.Threading.Tasks;
using OSharp.Core.Context;
using OSharp.Utility.Data;
using OSharp.Utility.Extensions;
using OSharp.Web.Http.Messages;
using OSharp.Web.Http;
using System.ComponentModel;
using System.Configuration;
using OSharp.Web.Http.Authentication;
using System.Linq;
using OSharp.Core.Data.Extensions;
using System.Web.Mvc;
using System.Web.Security;
using System.Web;
using Bode.Plugin.WebControles.MvcCaptcha;
using CDKX.Services.Core.Dtos.User;
using CDKX.Services.Core.Models.User;

namespace CDKX.Web.Areas.Web.Controllers
{
    [Description("Web账户接口")]
    public class AccountController : WebController
    {



        #region 账户相关业务

        /// <summary>
        /// 获取短信验证码
        /// </summary>
        /// <param name="phoneNo">手机号</param>
        /// <param name="codeType">1:注册;2:修改密码;3:修改手机号</param>
        [HttpPost]
        [Description("获取短信验证码")]
        public async Task<ActionResult> GetSmsCode(string phoneNo, CodeType codeType = CodeType.用户注册)
        {
            if (!phoneNo.IsMobileNumber(true)) return Json(new ApiResult(OperationResultType.ValidError, "请输入正确的手机号"));
            var result = await UserContract.GetSmsValidateCode(phoneNo, codeType);
            return Json(result.ToApiResult());
        }


        [HttpPost]
        [Description("获取邮箱验证码")]
        public async Task<ActionResult> GetEmailCode(string email, CodeType codeType = CodeType.用户注册)
        {
            if (!email.IsEmail()) return Json(new ApiResult(OperationResultType.ValidError, "请输入正确的邮箱地址"));
            var result = await UserContract.GetEmailValidateCode(email, codeType);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        [Description("绑定邮箱")]
        public ActionResult ChangeEmail(string validateCode, string nweEmail, string userName)
        {

            if (OperatorId == 0) return Json(new ApiResult("身份信息错误", OperationResultType.Error));
            var result = UserContract.ChangeEmail(validateCode, nweEmail, userName);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        [Description("单独验证手机验证码")]
        public ActionResult IsValidateCode(string phoneNo, string validateCode, CodeType type)
        {
            var result = UserContract.ValidatePhone(phoneNo, validateCode, type);
            return Json(result.ToApiResult());
        }


        //[HttpPost]
        //[Description("验证并注册")]
        //public async Task<IHttpActionResult> ValidateRegister(UserInfoRegistDto dto, string validateCode)
        //{
        //    var result = await UserContract.ValidateRegister(dto, validateCode);
        //    return Json(result.ToApiResult());
        //}

        [Description("验证并注册")]
        [HttpPost, ValidateMvcCaptcha(IsRetainSession = true)]
        public async Task<ActionResult> ValidateRegister(FormCollection values)
        {
            if (ModelState.IsValid)
            {
                var dto = new UserInfoRegistDto
                {
                    UserName = values["phoneNo"],
                    Password = values["password"],
                    NickName = values["phoneNo"]
                };
                var result = await UserContract.ValidateRegister(dto, values["validateCode"]);
                if (result.Successed)
                {
                    FormsAuthentication.SetAuthCookie(result.Data.ToJsonString(), false);
                }
                return Json(result.ToApiResult());
            }
            return Json(new { ReturnCode = 2, ReturnMsg = "图片验证码有误" });
        }


        [Description("用户登录")]
        [HttpPost, ValidateMvcCaptcha(IsRetainSession = true)]
        public async Task<ActionResult> Login(FormCollection values, string loginPhone, string loginPassword, int type, LoginDevice loginDevice, string clientVersion = "1.0.0", string registKey = "")
        {
            if (ModelState.IsValid)
            {
                var result = await UserContract.Login(loginPhone, loginPassword, registKey, loginDevice, clientVersion, type);
                if (!result.Successed)
                {
                    return Json(result.ToApiResult());
                }
                else
                {
                    var userId = (result.Data as UserTokenDto).Id;
                    var userString = UserContract.UserInfos.SingleOrDefault(m => m.Id == userId).SysUser.ToJsonString();
                    FormsAuthentication.SetAuthCookie(userString, false);
                }
                return Json(result.ToApiResult());
            }
            return Json(new { ReturnCode = 2, ReturnMsg = "图片验证码有误" });
        }


        [HttpPost]
        [Description("获取验证码")]
        public async Task<ActionResult> GetCode(string phoneNo, CodeType type)
        {
            var result = await UserContract.GetSmsValidateCode(phoneNo, type);
            return Json(result.Message);
        }

        [HttpPost]
        [TokenAuth]
        [Description("启动时重置Token过期时间")]
        public async Task<ActionResult> ResetTokenValidityPeriod(LoginDevice loginDevice, string clientVersion)
        {
            var user = await UserContract.UserInfos.SingleOrDefaultAsync(p => p.Id == OperatorId);
            if (user == null) return Json(new ApiResult("用户不存在", OperationResultType.QueryNull));

            var result = await UserContract.ResetToken(user, loginDevice, clientVersion);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        [Description("重置密码")]
        public async Task<ActionResult> ResetPassword(string userName, string newPsw, string validateCode)
        {
            var result = await UserContract.ResetPassword(userName, newPsw, validateCode);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        [TokenAuth]
        [Description("修改密码")]
        public async Task<ActionResult> ChangePassword(string userName, string oldPsw, string newPsw)
        {
            var result = await UserContract.ChangePassword(userName, oldPsw, newPsw);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        [Description("修改用户名")]
        public async Task<ActionResult> ChangeUserName(string userName, string newUserName, string password, string validateCode)
        {
            if (OperatorId == 0) return Json(new ApiResult("身份信息错误", OperationResultType.Error));
            var result = await UserContract.ChangeUserName(userName, newUserName, password, validateCode);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        // [TokenAuth]
        [Description("编辑用户信息")]
        public async Task<ActionResult> EditUserInfo(UserInfoEditDto dto)
        {
            dto.Id = OperatorId;
            var result = await UserContract.EditUserInfos(dto);
            return Json(result.ToApiResult());
        }

        [HttpPost]
        [TokenAuth]
        [Description("获取自己个人信息")]
        public async Task<ActionResult> GetUserInfo()
        {
            var theUser = await UserContract.UserInfos.Unrecycled().Include(p => p.SysUser).SingleOrDefaultAsync(p => p.Id == OperatorId);
            if (theUser == null) return Json(new ApiResult(OperationResultType.QueryNull, "用户不存在"));

            var userData = new
            {
                PhoneNo = theUser.SysUser.UserName
            };
            return Json(new ApiResult("获取成功", userData));
        }

        [HttpPost]
        [TokenAuth]
        [Description("意见反馈")]
        public async Task<ActionResult> AddFeedBack(string content)
        {
            var dto = new FeedBackDto { UserInfoId = OperatorId, Content = content };
            var result = await UserContract.SaveFeedBacks(dtos: dto);

            return Json(new ApiResult(result.ResultType, "反馈成功"));
        }

        #endregion
    }
}
