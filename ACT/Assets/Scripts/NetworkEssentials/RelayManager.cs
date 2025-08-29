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
using Unity.Services.Vivox;

public class RelayManager : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private GameObject thingsToDisable;
    [SerializeField] private Image panelImage;


    private async void Start()
    {
        try
        {
            // Initialize Unity Services with Vivox credentials
            // SetVivoxCredentials parameter order: (serverUrl, domain, issuer, key)
            var options = new InitializationOptions();
            options.SetVivoxCredentials(
                "https://unity.vivox.com/appconfig/57726-act-17813-udash",  // Server URL (first parameter)
                "mtu1xp.vivox.com",           // Domain (second parameter)
                "57726-act-17813-udash",      // Token Issuer (third parameter)
                ""                            // Token Key (fourth parameter, empty in your config)
            );
            await UnityServices.InitializeAsync(options);
            Debug.Log("Unity Services initialized with Vivox credentials");
            
            // Then sign in for Relay
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            // Finally initialize Vivox
            if (VivoxService.Instance != null)
            {
                try
                {
                    await VivoxService.Instance.InitializeAsync();
                    Debug.Log("Vivox initialized successfully");
                }
                catch (System.Exception vivoxEx)
                {
                    Debug.LogError($"Vivox initialization failed: {vivoxEx.Message}");
                }
            }
            else
            {
                Debug.LogWarning("VivoxService.Instance is null during Start; voice will be unavailable in this session.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Initialization error: {ex.Message}");
        }
    }

    public async void LoginToVivoxAsync()
    {
        //LoginOptions options = new LoginOptions();
       // options.DisplayName = "Player";
        //options.EnableTTS = true;
        await VivoxService.Instance.LoginAsync();
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

        bool started = NetworkManager.Singleton.StartHost();
        if (!started)
        {
            return null;
        }

        // Vivox: only login/join after host started
        if (VivoxService.Instance != null)
        {
            try
            {
                string hostName = $"host-{System.DateTime.UtcNow.Ticks % 10000}";
                await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = hostName });
                await WaitForVivoxLoginAsync();
                string channelToJoin = "World";
                // Use positional channel for proximity audio
                var channel3DProperties = new Channel3DProperties(50, 5, 50, AudioFadeModel.InverseByDistance);
                var channelOptions = new ChannelOptions();
                await VivoxService.Instance.JoinPositionalChannelAsync(channelToJoin, ChatCapability.AudioOnly, channel3DProperties, channelOptions);
                Debug.Log($"Vivox host logged in: {VivoxService.Instance.IsLoggedIn}, joined positional channel: {channelToJoin}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Vivox host voice error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("VivoxService.Instance is null post-host start; skipping voice join.");
        }

        return joinCode;

    }
    
    private async Task<bool> StartClientWithRelay(string joinCode) 
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join code is empty.");
            return false;
        }

        joinCode = joinCode.Trim();
        Debug.Log($"Attempting Relay join with code: '{joinCode}'");

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

        bool started = NetworkManager.Singleton.StartClient();
        if (!started)
        {
            return false;
        }

        // Vivox: only login/join after client started
        if (VivoxService.Instance != null)
        {
            try
            {
                string clientName = $"client-{System.DateTime.UtcNow.Ticks % 10000}";
                await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = clientName });
                await WaitForVivoxLoginAsync();
                string channelToJoin = "World";
                // Join the same positional channel as the host
                var channel3DProperties = new Channel3DProperties(50, 5, 50, AudioFadeModel.InverseByDistance);
                var channelOptions = new ChannelOptions();
                await VivoxService.Instance.JoinPositionalChannelAsync(channelToJoin, ChatCapability.AudioOnly, channel3DProperties, channelOptions);
                Debug.Log($"Vivox client logged in: {VivoxService.Instance.IsLoggedIn}, joined positional channel: {channelToJoin}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Vivox client voice error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("VivoxService.Instance is null post-client start; skipping voice join.");
        }

        return true;
    }

    private async Task WaitForVivoxLoginAsync(int timeoutMs = 3000)
    {
        var start = System.DateTime.UtcNow;
        while (!VivoxService.Instance.IsLoggedIn)
        {
            if ((System.DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                break;
            }
            await Task.Delay(100);
        }
    }
}
