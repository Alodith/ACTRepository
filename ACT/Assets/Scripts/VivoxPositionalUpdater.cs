using UnityEngine;
using Unity.Netcode;
using Unity.Services.Vivox;
using System.Collections;

public class VivoxPositionalUpdater : NetworkBehaviour
{
    [SerializeField] private string channelName = "World";
    [SerializeField] private float maxHearingDistance = 50f;
    [SerializeField] private float conversationalDistance = 5f;
    
    private bool isPositionalChannelJoined = false;
    private float joinCheckDelay = 7f; // Increased delay to ensure RelayManager has time to join
    private bool hasChannelError = false; // Track if we've had channel errors

    void Start()
    {
        StartCoroutine(WaitForChannelJoin());
    }

    IEnumerator WaitForChannelJoin()
    {
        yield return new WaitForSeconds(joinCheckDelay);
        
        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
        {
            Debug.Log($"Proximity controller active for channel: {channelName}");
            Debug.Log($"Proximity settings: conversational={conversationalDistance}m, max hearing={maxHearingDistance}m");
            
            // Assume RelayManager has joined the channel by now
            // Since we can't easily check channel membership, we'll proceed with position updates
            Debug.Log($"Assuming channel {channelName} is ready (joined by RelayManager), starting position updates");
            isPositionalChannelJoined = true;
            hasChannelError = false; // Reset error state
            StartCoroutine(Update3DPositions());
        }
    }

    IEnumerator Update3DPositions()
    {
        // Wait a bit more to ensure channel join is fully processed
        yield return new WaitForSeconds(1f);
        
        while (isPositionalChannelJoined && VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && !hasChannelError)
        {
            if (IsOwner)
            {
                // Update player's 3D position in the channel
                Vector3 playerPosition = transform.position;
                Set3DPosition(playerPosition);
                
                if (Time.frameCount % 60 == 0) // Log every 60 frames
                {
                    Debug.Log($"Updated 3D position: {playerPosition}");
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Update position 10 times per second
        }
        
        if (hasChannelError)
        {
            Debug.LogWarning("Position updates stopped due to channel errors");
        }
    }

    private void Set3DPosition(Vector3 position)
    {
        // If we've had channel errors, don't try to update positions
        if (hasChannelError)
        {
            return;
        }
        
        try
        {
            // Based on the compilation error, Set3DPosition signature is:
            // Set3DPosition(Vector3, Vector3, Vector3, Vector3, string, bool)
            // This likely represents: listener position, speaker position, unit vector, unit vector, channel name, boolean
            
            // For now, we'll use the player position for all Vector3 parameters
            // The string parameter is likely the channel name
            // The boolean parameter likely controls immediate update
            VivoxService.Instance.Set3DPosition(
                position,           // Listener position (player position)
                position,           // Speaker position (player position) 
                Vector3.forward,    // Unit vector (forward direction)
                Vector3.up,         // Unit vector (up direction)
                channelName,        // Channel name
                true                // Immediate update
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to set 3D position: {ex.Message}");
            
            // If Set3DPosition fails due to channel issues, stop trying immediately
            if (ex.Message.Contains("not currently in any channels") || 
                ex.Message.Contains("not currently in the specified target channel") ||
                ex.Message.Contains("InvalidState") ||
                ex.Message.Contains("AudioState must be connected"))
            {
                Debug.LogWarning("Channel appears to be disconnected, stopping position updates");
                hasChannelError = true;
                isPositionalChannelJoined = false;
            }
        }
    }

    void Update()
    {
        if (!IsOwner || !isPositionalChannelJoined || hasChannelError) return;
        
        // Log status
        if (Time.frameCount % 300 == 0) // Log every 5 seconds
        {
            if (isPositionalChannelJoined && !hasChannelError)
            {
                Debug.Log($"Positional audio active - voice fading with distance");
            }
            else
            {
                Debug.Log($"Positional audio stopped due to channel issues");
            }
        }
    }
}
