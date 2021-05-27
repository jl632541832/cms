﻿using System.Threading.Tasks;
using SSCMS.Core.Plugins;
using SSCMS.Core.Utils;

namespace SSCMS.Cli.Services
{
    public partial class ApiService
    {
        public async Task<(bool success, string failureMessage)> RegisterAsync(string userName, string mobile, string email, string password)
        {
            var url = GetCliUrl(RestUrlRegister);
            return await RestUtils.PostAsync(url, new RegisterRequest
            {
                UserName = userName,
                Mobile = mobile,
                Email = email,
                Password = password
            });
            //var client = new RestClient(CloudUtils.Api.GetCliUrl(RestUrlRegister)) { Timeout = -1 };
            //var request = new RestRequest(Method.POST);
            //request.AddHeader("Content-Type", "application/json");
            //request.AddParameter("application/json", TranslateUtils.JsonSerialize(new RegisterRequest
            //{
            //    UserName = userName,
            //    Mobile = mobile,
            //    Email = email,
            //    Password = password
            //}), ParameterType.RequestBody);
            //var response = client.Execute(request);

            //return response.IsSuccessful ? (true, null) : (false, StringUtils.Trim(response.Content, '"'));
        }
    }
}
