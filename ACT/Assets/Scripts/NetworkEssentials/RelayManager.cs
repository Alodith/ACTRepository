using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using TMPro;

public class RelayManager : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private GameObject thingsToDisable;
    [SerializeField] private Image panelImage;
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartRelay()
    {
        string joinCode = await StartHostWithRelay();
        joinCodeText.text = joinCode;
        thingsToDisable.SetActive(false);
        panelImage.enabled =false;
    }

    public async void JoinRelay()
    {
        await StartClientWithRelay(joinCodeInputField.text);
        thingsToDisable.SetActive(false);
        panelImage.enabled = false;
    }


    private async Task<string> StartHostWithRelay(int maxConnections = 3) 
    {

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

        //NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls")); <- this is old
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls")); //this works?
        //AllocationUtils.ToRelayServerData(allocation, "dtls"); <- they changed to this

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        return NetworkManager.Singleton.StartHost() ? joinCode : null;

    }
    
    private async Task<bool> StartClientWithRelay(string joinCode) 
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
}
