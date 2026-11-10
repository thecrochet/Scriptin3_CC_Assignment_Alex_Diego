using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YetAnotherCharacterSystem.Deprecated
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerLobbyManager : MonoBehaviour
    {
        public float MaxNumberPlayers = 4;

        // This is a property! See how it's different from a field variable?
        // It is set as an auto-property, with a public get and a private set. This let anyone read the value, but not modify it!
        public static PlayerLobbyManager Instance { get; private set; }

        public Dictionary<PlayerInput, PlayerInputController> PlayerList { get; private set; } = new Dictionary<PlayerInput, PlayerInputController>();

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        // OnEnable happens after the Awake method and before the Start method
        // It is the best location for subscribing and unsubscribing to events, which we are doing here
        private void OnEnable()
        {
            RegisterInstance();

            PlayerInputManager.instance.onPlayerJoined += AddPlayer;
            PlayerInputManager.instance.onPlayerLeft += RemovePlayer;
        }

        private void OnDisable()
        {
            UnregisterInstance();

            PlayerInputManager.instance.onPlayerJoined -= AddPlayer;
            PlayerInputManager.instance.onPlayerLeft -= RemovePlayer;
        }

        private void RegisterInstance()
        {
            if (Instance == null) 
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple PlayerLobbyManager components are currently loaded. There should only be one PlayerLobbyManager", this);
                return;
            }
        }

        private void UnregisterInstance()
        {
            if (Instance == this) Instance = null;
        }

        public void AddPlayer(PlayerInput playerInput)
        {
            PlayerInputController playerInputController = playerInput.GetComponent<PlayerInputController>();

            if (playerInputController != null)
            {
                PlayerList.Add(playerInput, playerInput.GetComponent<PlayerInputController>());
            }
            else
            {
                Debug.LogError("Error spawning new player prefab - It must include a Player Input Controller component.");
                Destroy(playerInput.gameObject);
            }
        }

        public void RemovePlayer(PlayerInput playerInput)
        {
            PlayerList.Remove(playerInput);
        }
    }
}