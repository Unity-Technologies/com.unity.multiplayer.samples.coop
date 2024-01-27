using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services;
using UnityEngine.SceneManagement;

namespace PanicBuying
{
    [CreateAssetMenu(fileName = "NetworkState", menuName = "PanicBuying/Network")]
    public class NetworkState : ScriptableObject
    {
        public enum Type
        {
            None,
            Host,
            Client,
        };

        public string JoinCode { get; private set; } = null;

        public string PlayerId { get; private set; } = null;

        public Type NetworkType { get; private set; } = Type.None;

        public Allocation HostAllocation { get; private set; } = null;

        public JoinAllocation JoinAllocation { get; private set; } = null;


        private System.Guid _allocationId = System.Guid.Empty;

        private EventListener<CreateRoomButtonClicked> createRoomListener = new();
        private EventListener<JoinRoomSubmited> joinRoomListener = new();
        //private EventListener<GameOver> gameOberListener = new();


        public async void Initialize()
        {
            await UnityServices.InitializeAsync();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PlayerId = AuthenticationService.Instance.PlayerId;

            createRoomListener.StartListen(async (e) =>
            {
                var allRegions = await RelayService.Instance.ListRegionsAsync();

                if (allRegions.Count <= 0)
                {
                    Debug.LogError("Failed to load accessable region");
                    return;
                }

                var region = allRegions[0];


                try
                {
                    this.NetworkType = Type.Host;
                    this.HostAllocation = await RelayService.Instance.CreateAllocationAsync(4, region.Id);
                    this.JoinCode = await RelayService.Instance.GetJoinCodeAsync(this.HostAllocation.AllocationId);

                    SceneManager.LoadScene($"NetworkTest");
                }
                catch (RelayServiceException ex)
                {
                    Debug.LogError(ex.Message + "\n" + ex.StackTrace);
                }
            });

            joinRoomListener.StartListen(async (e) =>
            {
                JoinCode = e.code;

                try
                {
                    this.NetworkType = Type.Client;
                    this.JoinAllocation = await RelayService.Instance.JoinAllocationAsync(e.code);

                    SceneManager.LoadScene($"NetworkTest");
                }
                catch (RelayServiceException ex)
                {
                    Debug.LogError(ex.Message + "\n" + ex.StackTrace);
                }
            });
        }
    }
}
