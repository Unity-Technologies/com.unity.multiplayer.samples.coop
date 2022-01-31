using System;
using System.Threading.Tasks;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using RelayService = Unity.Services.Relay.Relay;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Relays
{
    /// <summary>
    /// Wrapper for all the interaction with the Relay API.
    /// Relay acts as an intermediary between hosts and clients for privacy. Each player will connect to an obfuscated IP address provided by Relay as though connecting directly to other players.
    /// </summary>
    public class RelayAPIInterface
    {
        private readonly IPublisher<UnityServiceErrorMessage> m_ErrorPopupPublisher;

        private void DoRequest(Task task, Action onComplete)
        {
            AsyncUnityServiceRequest.DoRequest<RelayServiceException>(task, onComplete, ParseServiceException);
        }

        private void DoRequest<T>(Task<T> task, Action<T> onComplete)
        {
            AsyncUnityServiceRequest.DoRequest<T,RelayServiceException>(task, onComplete, ParseServiceException);
        }

        [Inject]
        public RelayAPIInterface(IPublisher<UnityServiceErrorMessage> errorPopupPublisher)
        {
            m_ErrorPopupPublisher = errorPopupPublisher;
        }


        /// <summary>
        /// A Relay Allocation represents a "server" for a new host.
        /// </summary>
        public void AllocateAsync(int maxConnections, Action<Allocation> onComplete)
        {
            var task = RelayService.Instance.CreateAllocationAsync(maxConnections);
            DoRequest(task, OnResponse);

            void OnResponse(Allocation response)
            {
                if (response == null)
                    Debug.LogError("Relay returned a null Allocation. This might occur if the Relay service has an outage, if your cloud project ID isn't linked, or if your Relay package version is outdated.");
                else
                    onComplete?.Invoke(response);
            };
        }

        private void ParseServiceException(RelayServiceException e)
        {
            var reason = e.Reason == RelayExceptionReason.Unknown ?
                "Relay Error: Relay service had an unknown error." :
                $"Relay Error: {e.Message}";
            m_ErrorPopupPublisher.Publish(new UnityServiceErrorMessage(reason));
        }

        /// <summary>
        /// Only after an Allocation has been completed can a Relay join code be obtained. This code will be stored in the lobby's data as non-public
        /// such that players can retrieve the Relay join code only after connecting to the lobby. (Note that this is not the same as the lobby code.)
        /// </summary>
        public void GetJoinCodeAsync(Guid hostAllocationId, Action<string> onComplete)
        {
            var task = RelayService.Instance.GetJoinCodeAsync(hostAllocationId);
            DoRequest(task, OnResponse);

            void OnResponse(string response)
            {
                if (response == null)
                    Debug.LogError("Could not retrieve a Relay join code.");
                else
                    onComplete?.Invoke(response);
            }
        }

        /// <summary>
        /// Clients call this to retrieve the host's Allocation via a Relay join code.
        /// </summary>
        public void JoinAsync(string joinCode, Action<JoinAllocation> onComplete)
        {
            var task = RelayService.Instance.JoinAllocationAsync(joinCode);
            DoRequest(task, OnResponse);

            void OnResponse(JoinAllocation response)
            {
                if (response == null)
                    Debug.LogError("Could not join async with Relay join code " + joinCode);
                else
                    onComplete?.Invoke(response);
            };
        }
    }
}
