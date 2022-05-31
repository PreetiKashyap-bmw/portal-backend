﻿using CatenaX.NetworkServices.Keycloak.Authentication;
using CatenaX.NetworkServices.Provisioning.Library.Models;
using CatenaX.NetworkServices.Registration.Service.BusinessLogic;
using CatenaX.NetworkServices.Registration.Service.Model;
using CatenaX.NetworkServices.PortalBackend.DBAccess.Models;
using CatenaX.NetworkServices.PortalBackend.PortalEntities.Enums;
using CatenaX.NetworkServices.Framework.ErrorHandling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using System.Net;

namespace CatenaX.NetworkServices.Registration.Service.Controllers
{
    [ApiController]
    [Route("api/registration")]
    public class RegistrationController : ControllerBase
    {
        private readonly ILogger<RegistrationController> _logger;
        private readonly IRegistrationBusinessLogic _registrationBusinessLogic;

        public RegistrationController(ILogger<RegistrationController> logger, IRegistrationBusinessLogic registrationBusinessLogic)
        {
            _logger = logger;
            _registrationBusinessLogic = registrationBusinessLogic;
        }

        [HttpGet]
        [Authorize(Roles = "add_company_data")]
        [Route("company/{bpn}")]
        [ProducesResponseType(typeof(Company), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOneObjectAsync([FromRoute] string bpn, [FromHeader] string authorization)
        {
            try
            {
                return Ok(await _registrationBusinessLogic.GetCompanyByIdentifierAsync(bpn, authorization.Split(" ")[1]).ConfigureAwait(false));
            }
            catch (ServiceException e)
            {
                var content = new { message = e.Message };
                return new ContentResult { StatusCode = (int)e.StatusCode, Content = JsonConvert.SerializeObject(content), ContentType = "application/json" };
            }
        }

        [HttpPost]
        [Authorize(Roles = "upload_documents")]
        [Route("application/{applicationId}/documentType/{documentTypeId}/documents")]
        public Task<int> UploadDocumentAsync([FromRoute] Guid applicationId, [FromRoute] DocumentTypeId documentTypeId, [FromForm(Name = "document")] IFormFile document) =>
            this.WithIamUserId(user => _registrationBusinessLogic.UploadDocumentAsync(applicationId, document, documentTypeId, user));


        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("application/{applicationId}/documentType/{documentTypeId}/documents")]
        public IAsyncEnumerable<UploadDocuments> GetUploadedDocumentsAsync([FromRoute] Guid applicationId,[FromRoute] DocumentTypeId documentTypeId) =>
           this.WithIamUserId(user => _registrationBusinessLogic.GetUploadedDocumentsAsync(applicationId,documentTypeId,user));

        [HttpPut]
        [Authorize(Roles = "invite_user")]
        [Route("idp")]
        public async Task<IActionResult> SetIdpAsync([FromBody] SetIdp idpToSet)
        {
            await _registrationBusinessLogic.SetIdpAsync(idpToSet).ConfigureAwait(false);
            return Ok();
        }

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("rolesComposite")]
        [ProducesResponseType(typeof(List<string>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetClientRolesComposite()
        {
            try
            {
                var result = await _registrationBusinessLogic.GetClientRolesCompositeAsync().ConfigureAwait(false);
                return Ok(result.ToList());

            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("applications")]
        public IAsyncEnumerable<CompanyApplication> GetApplicationsWithStatusAsync() =>
            this.WithIamUserId(user => _registrationBusinessLogic.GetAllApplicationsForUserWithStatus(user));

        [HttpPut]
        [Authorize(Roles = "submit_registration")]
        [Route("application/{applicationId}/status")]
        public Task<int> SetApplicationStatusAsync([FromRoute] Guid applicationId, [FromQuery] CompanyApplicationStatusId status) =>
            _registrationBusinessLogic.SetApplicationStatusAsync(applicationId, status);

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("application/{applicationId}/status")]
        public Task<CompanyApplicationStatusId> GetApplicationStatusAsync([FromRoute] Guid applicationId) =>
            _registrationBusinessLogic.GetApplicationStatusAsync(applicationId);

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("application/{applicationId}/companyDetailsWithAddress")]
        public Task<CompanyWithAddress> GetCompanyWithAddressAsync([FromRoute] Guid applicationId) =>
            _registrationBusinessLogic.GetCompanyWithAddressAsync(applicationId);

        [HttpPost]
        [Authorize(Roles = "add_company_data")]
        [Route("application/{applicationId}/companyDetailsWithAddress")]
        public Task SetCompanyWithAddressAsync([FromRoute] Guid applicationId, [FromBody] CompanyWithAddress companyWithAddress) =>
            _registrationBusinessLogic.SetCompanyWithAddressAsync(applicationId, companyWithAddress);

        [HttpPost]
        [Authorize(Roles = "invite_user")]
        [Route("application/{applicationId}/inviteNewUser")]
        public Task<int> InviteNewUserAsync([FromRoute] Guid applicationId, [FromBody] UserCreationInfo userCreationInfo) =>
            this.WithIamUserId(iamUserId =>
                _registrationBusinessLogic.InviteNewUserAsync(applicationId, userCreationInfo, iamUserId));

        [HttpPost]
        [Authorize(Roles = "submit_registration")]
        [Route("application/{applicationId}/companyRoleAgreementConsents")]
        public Task<int> SubmitCompanyRoleConsentToAgreementsAsync([FromRoute] Guid applicationId, [FromBody] CompanyRoleAgreementConsents companyRolesAgreementConsents) =>
            this.WithIamUserId(iamUserId =>
                _registrationBusinessLogic.SubmitRoleConsentAsync(applicationId, companyRolesAgreementConsents, iamUserId));

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("application/{applicationId}/companyRoleAgreementConsents")]
        public Task<CompanyRoleAgreementConsents> GetAgreementConsentStatusesAsync([FromRoute] Guid applicationId) =>
            this.WithIamUserId(iamUserId =>
                _registrationBusinessLogic.GetRoleAgreementConsentsAsync(applicationId, iamUserId));

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("companyRoleAgreementData")]
        public Task<CompanyRoleAgreementData> GetCompanyRoleAgreementDataAsync() =>
            _registrationBusinessLogic.GetCompanyRoleAgreementDataAsync();

        [HttpPost]
        [Authorize(Roles = "submit_registration")]
        [Route("submitregistration")]
        public async Task<IActionResult> SubmitRegistrationAsync()
        {
            try
            {
                var userEmail = User.Claims.SingleOrDefault(x => x.Type == "email").Value as string;

                if (await _registrationBusinessLogic.SubmitRegistrationAsync(userEmail).ConfigureAwait(false))
                {
                    return Ok();
                }
                _logger.LogError("unsuccessful");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Authorize(Roles = "view_registration")]
        [Route("application/{applicationId}/invitedusers")]
        public IAsyncEnumerable<InvitedUser> GetInvitedUsersAsync([FromRoute] Guid applicationId) =>
            _registrationBusinessLogic.GetInvitedUsersAsync(applicationId);

        [HttpPut]
        [Authorize(Roles = "view_registration")]
        [Route("invitation/status")]
        public Task<int> SetInvitationStatusAsync() =>
           this.WithIamUserId(iamUserId =>
                _registrationBusinessLogic.SetInvitationStatusAsync(iamUserId));

    }
}
