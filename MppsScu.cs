using System;
using ClearCanvas.Common;
using ClearCanvas.Dicom.Iod.Iods;
using ClearCanvas.Dicom.Iod.Modules;

namespace ClearCanvas.Dicom.Network.Scu
{
    /// <summary>
    /// Scu class for MPPS.
    /// <example>
    /// For Send MPPS InProgress status.
    /// <code><![CDATA[
    /// var mppsScu = new MppsScu();
    /// ModalityPerformedProcedureStepIod mppsIod = new ModalityPerformedProcedureStepIod();
    /// Set all the mandatory files mentioned in the MPPS DICOM Standard 
    /// mppsIod.SetCommonTags();
    /// var dicomState = mppsScu.SendMPPSStatus("myClientAeTitle","CONQUESTDLBPC", "127.0.0.1", 5678, mppsIod, PerformedProcedureStepStatus.InProgress);
    /// if (dicomState == ClearCanvas.Dicom.Network.DicomState.Success)
    ///     affectedSopInstanceUid = mppsScu.AffectedSopInstanceUid.UID;
    /// ]]></code>
    /// </example>
    /// <example>
    /// For Send MPPS Completed (or) Discontinued status.
    /// <code><![CDATA[
    /// var mppsScu = new MppsScu();
    /// ModalityPerformedProcedureStepIod mppsIod = new ModalityPerformedProcedureStepIod();
    /// Set all the mandatory files mentioned in the MPPS DICOM Standard 
    /// mppsIod.SetCommonTags();
    /// var dicomState = mppsScu.SendMPPSStatus("myClientAeTitle","CONQUESTDLBPC", "127.0.0.1", 5678, mppsIod, PerformedProcedureStepStatus.Completed,affectedSopInstanceUid);
    /// if (dicomState == ClearCanvas.Dicom.Network.DicomState.Success)
    ///     affectedSopInstanceUid = mppsScu.AffectedSopInstanceUid.UID;
    /// ]]></code>
    /// </example>
    /// </summary>
    public class MppsScu : ScuBase
    {
        #region Private Variables
        private ModalityPerformedProcedureStepIod _procedureStepIod;
        #endregion

        #region Public Methods/Properties
        /// <summary>
        /// N-Create Response SopInstanceUid.
        /// </summary>
        public DicomUid AffectedSopInstanceUid { get; set; }
        /// <summary>
        /// MPPS InProgress, Completed (or) Discontinued.
        /// </summary>
        public PerformedProcedureStepStatus ProcedureStepStatus { set; get; }

        /// <summary>
        /// Sends the Modality Performed ProcedureStep status to RIS or PACS.
        /// </summary>
        /// <param name="clientAETitle">The client AE title.</param>
        /// <param name="remoteAE">The remote AE.</param>
        /// <param name="remoteHost">The remote host.</param>
        /// <param name="remotePort">The remote port.</param>
        /// <param name="procedureStepIod">The Procedure Step to Send.</param>
        /// <param name="procedureStepStatus">PerformedProcedureStepStatus</param>
        /// <param name="affectedSopInstanceUid">AffectedSopInstanceUid required for MPPS Completed (or) Discontinued</param>
        public DicomState SendMPPSStatus(string clientAETitle, string remoteAE, string remoteHost, int remotePort,
            ModalityPerformedProcedureStepIod procedureStepIod, PerformedProcedureStepStatus procedureStepStatus, string affectedSopInstanceUid = null)
        {
            _procedureStepIod = procedureStepIod;
            ProcedureStepStatus = procedureStepStatus;
            AffectedSopInstanceUid = affectedSopInstanceUid == null ? null : new DicomUid(affectedSopInstanceUid, "Instance UID", UidType.SOPInstance);

            Connect(clientAETitle, remoteAE, remoteHost, remotePort);
            if (Status == ScuOperationStatus.Canceled)
                return DicomState.Cancel;
            if (Status == ScuOperationStatus.AssociationRejected || Status == ScuOperationStatus.Failed || Status == ScuOperationStatus.ConnectFailed ||
                Status == ScuOperationStatus.NetworkError || Status == ScuOperationStatus.TimeoutExpired)
                return DicomState.Failure;
            return ResultStatus;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sends the Modality Performed ProcedureStep STARTED Status.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="association">The association.</param>
        private void SendNCreateRequest(DicomClient client, ClientAssociationParameters association)
        {
            byte pcid = association.FindAbstractSyntaxWithTransferSyntax(SopClass.ModalityPerformedProcedureStepSopClass,
                                                                         TransferSyntax.ExplicitVrLittleEndian);
            if (pcid == 0)
                pcid = association.FindAbstractSyntaxWithTransferSyntax(SopClass.ModalityPerformedProcedureStepSopClass,
                                                                        TransferSyntax.ImplicitVrLittleEndian);
            if (pcid == 0)
            {
                client.SendAssociateAbort(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
                return;
            }

            if (pcid > 0)
            {
                var message = new DicomMessage(null, (DicomAttributeCollection)_procedureStepIod.DicomAttributeProvider);
                this.Client.SendNCreateRequest(null, AssociationParameters.FindAbstractSyntaxOrThrowException(SopClass.ModalityPerformedProcedureStepSopClass),
                    this.Client.NextMessageID(), message, DicomUids.ModalityPerformedProcedureStep);

                Platform.Log(LogLevel.Debug, "Creating ModalityPerformedProcedureStep Status...");
            }
        }
        /// <summary>
        /// Sends the Modality Performed ProcedureStep Completed or Canceled Status.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="association">The association.</param>
        private void SendNSetRequest(DicomClient client, ClientAssociationParameters association)
        {
            byte pcid = association.FindAbstractSyntaxWithTransferSyntax(SopClass.ModalityPerformedProcedureStepSopClass,
                                                                         TransferSyntax.ExplicitVrLittleEndian);
            if (pcid == 0)
                pcid = association.FindAbstractSyntaxWithTransferSyntax(SopClass.ModalityPerformedProcedureStepSopClass,
                                                                        TransferSyntax.ImplicitVrLittleEndian);
            if (pcid == 0)
            {
                client.SendAssociateAbort(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
                return;
            }

            if (pcid > 0)
            {
                var message = new DicomMessage(null, (DicomAttributeCollection)_procedureStepIod.DicomAttributeProvider)
                {
                    RequestedSopClassUid = SopClass.ModalityPerformedProcedureStepSopClassUid,
                    RequestedSopInstanceUid = AffectedSopInstanceUid.UID
                };
                this.Client.SendNSetRequest(AssociationParameters.FindAbstractSyntaxOrThrowException(SopClass.ModalityPerformedProcedureStepSopClass),
                    this.Client.NextMessageID(), message);

                Platform.Log(LogLevel.Debug, "Updating ModalityPerformedProcedureStep status...");
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Called when received associate accept.  here is where send the find request.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="association">The association.</param>
        public override void OnReceiveAssociateAccept(DicomClient client, ClientAssociationParameters association)
        {
            try
            {
                base.OnReceiveAssociateAccept(client, association);
                if (Canceled)
                    client.SendAssociateAbort(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
                else
                {
                    Platform.Log(LogLevel.Info, "Association Accepted:\r\n{0}", association.ToString());
                    if (ProcedureStepStatus == PerformedProcedureStepStatus.InProgress)
                        SendNCreateRequest(client, association);
                    else
                        SendNSetRequest(client, association);
                }
            }
            catch (Exception ex)
            {
                this.FailureDescription = ex.Message;
                Platform.Log(LogLevel.Error, ex.ToString());
                ReleaseConnection(client);
                throw;
            }
        }
        /// <summary>
        /// Called when received response message.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="association">The association.</param>
        /// <param name="presentationID">The presentation ID.</param>
        /// <param name="message">The message.</param>
        public override void OnReceiveResponseMessage(DicomClient client, ClientAssociationParameters association, byte presentationID, DicomMessage message)
        {
            try
            {
                this.ResultStatus = message.Status.Status;
                switch (this.ResultStatus)
                {
                    case DicomState.Cancel:
                    case DicomState.Pending:
                    case DicomState.Failure:
                        Platform.Log(LogLevel.Error, string.Format("{0} status received in MPPS Scu response message", message.Status.Status));

                        this.FailureDescription = "Unexpected failure reported by the MPPS SCP.";
                        this.ReleaseConnection(client);
                        return;

                    case DicomState.Warning:
                        Platform.Log(LogLevel.Warn, string.Format("{0} status received in MPPS Scu response message", message.Status.Status));
                        break;

                    case DicomState.Success:
                        break;
                }
                if (Canceled)
                {
                    Platform.Log(LogLevel.Info, "Cancel requested by user.  Closing association.");
                    client.SendAssociateAbort(DicomAbortSource.ServiceUser, DicomAbortReason.NotSpecified);
                    return;
                }

                Platform.Log(LogLevel.Info, "Success status received in MPPS Scu");
                AffectedSopInstanceUid = new DicomUid(message.AffectedSopInstanceUid, "Instance UID", UidType.SOPInstance);

                switch (message.CommandField)
                {
                    case DicomCommandField.NCreateResponse:
                        this.ReleaseConnection(client);
                        break;

                    case DicomCommandField.NDeleteResponse:
                        break;

                    case DicomCommandField.NSetResponse:
                        this.ReleaseConnection(client);
                        break;

                    case DicomCommandField.NActionResponse:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                this.FailureDescription = ex.Message;
                Platform.Log(LogLevel.Error, ex.ToString());
                ReleaseConnection(client);
                throw;
            }
        }
        /// <summary>
        /// Adds the appropriate presentation context.
        /// </summary>
        protected override void SetPresentationContexts()
        {
            byte pcid = AssociationParameters.FindAbstractSyntax(SopClass.ModalityPerformedProcedureStepSopClass);
            if (pcid == 0)
            {
                pcid = AssociationParameters.AddPresentationContext(SopClass.ModalityPerformedProcedureStepSopClass);

                AssociationParameters.AddTransferSyntax(pcid, TransferSyntax.ExplicitVrLittleEndian);
                AssociationParameters.AddTransferSyntax(pcid, TransferSyntax.ImplicitVrLittleEndian);
            }
        }
        #endregion

        #region IDisposable Members

        private bool _disposed;
        /// <summary>
        /// Disposes the specified disposing.
        /// </summary>
        /// <param name="disposing">if set to <c>true</c> [disposing].</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                // Dispose of other Managed objects, ie

            }
            // FREE UNMANAGED RESOURCES
            base.Dispose(true);
            _disposed = true;
        }
        #endregion
    }
}
