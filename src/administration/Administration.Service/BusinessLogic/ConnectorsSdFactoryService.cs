/********************************************************************************
 * Copyright (c) 2021,2022 BMW Group AG
 * Copyright (c) 2021,2022 Contributors to the CatenaX (ng) GitHub Organisation.
 *
 * See the NOTICE file(s) distributed with this work for additional
 * information regarding copyright ownership.
 *
 * This program and the accompanying materials are made available under the
 * terms of the Apache License, Version 2.0 which is available at
 * https://www.apache.org/licenses/LICENSE-2.0.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * SPDX-License-Identifier: Apache-2.0
 ********************************************************************************/

using System.Net.Http.Headers;
using System.Security.Cryptography;
using Org.CatenaX.Ng.Portal.Backend.Administration.Service.Models;
using Org.CatenaX.Ng.Portal.Backend.Framework.ErrorHandling;
using Org.CatenaX.Ng.Portal.Backend.PortalBackend.DBAccess;
using Org.CatenaX.Ng.Portal.Backend.PortalBackend.DBAccess.Repositories;
using Org.CatenaX.Ng.Portal.Backend.PortalBackend.PortalEntities.Entities;
using Org.CatenaX.Ng.Portal.Backend.PortalBackend.PortalEntities.Enums;
using Microsoft.Extensions.Options;

namespace Org.CatenaX.Ng.Portal.Backend.Administration.Service.BusinessLogic;

/// <summary>
/// Service to handle communication with the connectors sd factory
/// </summary>
public class ConnectorsSdFactoryService : IConnectorsSdFactoryService
{
    private readonly ConnectorsSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPortalRepositories _portalRepositories;

    /// <summary>
    /// Creates a new instance of <see cref="ConnectorsSdFactoryService"/>
    /// </summary>
    /// <param name="options">The options</param>
    /// <param name="httpClientFactory">Factory to create httpClients</param>
    /// <param name="portalRepositories">Access to the portal Repositories</param>
    public ConnectorsSdFactoryService(IOptions<ConnectorsSettings> options, IHttpClientFactory httpClientFactory, IPortalRepositories portalRepositories)
    {
        _httpClientFactory = httpClientFactory;
        _portalRepositories = portalRepositories;
        _settings = options.Value;
    }

    /// <inheritdoc />
    public async Task<Guid> RegisterConnectorAsync(ConnectorInputModel connectorInputModel, string accessToken, string bpn, string issuerBpn)
    {
        using var httpClient =_httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        // The hardcoded values (headquarterCountry, legalCountry, sdType, issuer) will be fetched from the user input or db in future
        var requestModel = new ConnectorSdFactoryRequestModel("ServiceOffering", connectorInputModel.ConnectorUrl,string.Empty, string.Empty, string.Empty, issuerBpn, bpn);
        var response = await httpClient.PostAsJsonAsync(_settings.SdFactoryUrl, requestModel).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new ServiceException($"Access to SD factory failed with status code {response.StatusCode}", response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var sha512Hash = SHA512.Create();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var hash = await sha512Hash.ComputeHashAsync(ms);
        var documentContent = ms.GetBuffer();
        if (ms.Length != stream.Length || documentContent.Length != stream.Length)
        {
            throw new ControllerArgumentException($"document transmitted length {stream.Length} doesn't match actual length {ms.Length}.");
        }

        void SetupOptionalFields(Document doc)
        {
            doc.DocumentTypeId = DocumentTypeId.SELF_DESCRIPTION_EDC;
        }

        var document = _portalRepositories.GetInstance<IDocumentRepository>().CreateDocument($"SelfDescription_{bpn}.json", documentContent, hash, SetupOptionalFields);
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
        return document.Id;
    }
}
