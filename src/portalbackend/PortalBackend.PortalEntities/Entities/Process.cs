/********************************************************************************
 * Copyright (c) 2022 Contributors to the Eclipse Foundation
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

using Org.Eclipse.TractusX.Portal.Backend.Framework.Processes.Library.Concrete.Entities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;

namespace Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Entities;

public class Process(Guid id, ProcessTypeId processTypeId, Guid version) : AbstractProcess<Process, ProcessTypeId, ProcessStepTypeId>(id, processTypeId, version)
{
    // Navigation properties
    public virtual CompanyApplication? CompanyApplication { get; set; }
    public virtual OfferSubscription? OfferSubscription { get; set; }
    public virtual NetworkRegistration? NetworkRegistration { get; set; }
    public virtual MailingInformation? MailingInformation { get; set; }
    public virtual CompanyInvitation? CompanyInvitation { get; set; }
    public virtual ExternalTechnicalUserCreationData? ExternalTechnicalUserCreationData { get; set; }
    public virtual CompanyUserAssignedProcess? CompanyUserAssignedProcess { get; set; }
    public virtual IdentityProviderAssignedProcess? IdentityProviderAssignedProcess { get; set; }
    public virtual Company? SdCreationCompany { get; set; }
    public virtual Connector? SdCreationConnector { get; set; }
}
