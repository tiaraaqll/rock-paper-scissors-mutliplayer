using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class CardGameManager : MonoBehaviour, IOnEventCallback
{
    public GameObject netPlayerPrefab;
    public CardPlayer P1;
    public CardPlayer P2;
    public float restoreValue=5;
    public float damageValue=10;
    public GameState State, NextState = GameState.NetPlayersIntialization;
    public GameObject gameOverPanel;
    public TMP_Text winnerText;
    public TMP_Text pingText;

    private CardPlayer damagedPlayer;
    private CardPlayer winner;
    public bool Online = true;

    //public List <int> syncReadyPlayers = new List<int>(2);
    HashSet<int> syncReadyPlayers= new HashSet<int>();

    // Enum State
    public enum GameState
    {
        SyncState,
        NetPlayersIntialization,
        ChooseAttack,
        Attack,
        Damages,
        Draw,
        GameOver
    }


    private void Start()
    {
        gameOverPanel.SetActive(false);
        if(Online) {
            PhotonNetwork.Instantiate(netPlayerPrefab.name, Vector3.zero, Quaternion.identity);
            StartCoroutine(PingCoroutine());
            State = GameState.NetPlayersIntialization;
            NextState = GameState.NetPlayersIntialization;

            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyNames.Room.RestoreValue, out var restoreValue)) {
                this.restoreValue = (float)restoreValue;
            }
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyNames.Room.DamageValue, out var damageValue)) {
                this.damageValue = (float)damageValue;
            }
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyNames.Room.MaxHealth, out var MaxHealth)) {
                P1.MaxHealth = (float)MaxHealth;
                P2.MaxHealth = (float)MaxHealth;
            }
        }
        else {
            State = GameState.ChooseAttack;
        }
        //* Mengecek posisi state
        //     Debug.Log(state == State.ChooseAttack);
        //     state = State.Damages;
        //     Debug.Log(state == State.ChooseAttack);
        //     Debug.Log(state);


    }

    private void Update()
    {
        // Logic gameplay
        switch (State)
        {
            case GameState.SyncState:
                if(syncReadyPlayers.Count == 2) 
                {
                    syncReadyPlayers.Clear();
                    State = NextState;
                }
                break;

            case GameState.NetPlayersIntialization:
                if(CardNetPlayer.NetPlayers.Count == 2) 
                {
                    foreach (var netPlayer in CardNetPlayer.NetPlayers)
                    {
                        if(netPlayer.photonView.IsMine) 
                        {
                            netPlayer.Set(P1);
                        }
                        else 
                        {
                            netPlayer.Set(P2);
                        }
                    }
                    ChangeState (GameState.ChooseAttack);
                }
                break;

            case GameState.ChooseAttack:
                if (P1.AttackValue != null && P2.AttackValue != null)
                {
                    P1.AnimateAttack();
                    P2.AnimateAttack();
                    P1.SetClickable(false);
                    P2.SetClickable(false);
                    ChangeState (GameState.Attack);
                }
                break;

            case GameState.Attack:
                if (P1.IsAnimating() == false && P2.IsAnimating() == false)
                {
                    damagedPlayer = GetDamagePlayer();
                    if (damagedPlayer != null)
                    {
                        damagedPlayer.AnimateDamage();
                        ChangeState (GameState.Damages);
                    }
                    else
                    {
                        P1.AnimateDraw();
                        P2.AnimateDraw();
                        ChangeState (GameState.Draw);
                    }
                }

                break;
            case GameState.Damages:
                if (P1.IsAnimating() == false && P2.IsAnimating() == false)
                {
                    //* Calculate Health
                    if (damagedPlayer == P1)
                    {
                        P1.ChangeHealth(-damageValue);
                        P2.ChangeHealth(restoreValue);
                    }
                    else
                    {
                        P2.ChangeHealth(restoreValue);
                        P1.ChangeHealth(-damageValue);
                    }

                    var winner = GetWinner();

                    if (winner == null)
                    {
                        ResetPlayers();
                        P1.SetClickable(true);
                        P2.SetClickable(true);
                        ChangeState (GameState.ChooseAttack);
                    }
                    else
                    {
                        gameOverPanel.SetActive(true);
                        winnerText.text = winner == P1 ? $"{P1.NickName.text} Wins" : $"{P2.NickName.text} Wins";
                        ResetPlayers();
                        ChangeState (GameState.GameOver);
                    }

                }
                break;
            case GameState.Draw:
                if (P1.IsAnimating() == false && P2.IsAnimating() == false)
                {
                    ResetPlayers();
                    P1.SetClickable(true);
                    P2.SetClickable(true);
                    ChangeState (GameState.ChooseAttack);
                }
                break;
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDinable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private const byte playerChangeState=1;

    private void ChangeState(GameState newState) {
        if(Online == false) {
            State = newState;
            return;
        }

        if(this.NextState == newState)
            return;

        //kirim message bahwa player ready
        var actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
        var raiseEventOptions = new RaiseEventOptions();
        raiseEventOptions.Receivers = ReceiverGroup.All;
        PhotonNetwork.RaiseEvent(1, actorNum, raiseEventOptions, SendOptions.SendReliable);

        //masuk ke state sync sebagai transisi sebelum state baru
        this.State =  GameState.SyncState;
        this.NextState = newState;
    }

    public void OnEvent (EventData photonEvent) {
        switch (photonEvent.Code)
        {
            case playerChangeState :
                var actorNum = (int) photonEvent.CustomData;

                //if(syncReadyPlayers.Contains(actorNum)==false);

                //kalau pake hashset ga perlu cek lagi
                syncReadyPlayers.Add(actorNum);
                
                break;
            default:
                break;
        }
   }

    IEnumerator PingCoroutine() {
        var wait = new WaitForSeconds(1);
        while(true) {
            pingText.text = "ping : " + PhotonNetwork.GetPing() + " ms";
            yield return wait;   
        }
    }
        

    private void ResetPlayers()
    {
        damagedPlayer = null;
        P1.Reset();
        P2.Reset();
    }

    private CardPlayer GetDamagePlayer()
    {
        Attack? PlayerAttack1 = P1.AttackValue;
        Attack? PlayerAttack2 = P2.AttackValue;

        if (PlayerAttack1 == Attack.Rock && PlayerAttack2 == Attack.Paper)
        {
            return P1;
        }
        else if (PlayerAttack1 == Attack.Rock && PlayerAttack2 == Attack.Scissor)
        {
            return P2;
        }
        else if (PlayerAttack1 == Attack.Paper && PlayerAttack2 == Attack.Rock)
        {
            return P2;
        }
        else if (PlayerAttack1 == Attack.Paper && PlayerAttack2 == Attack.Scissor)
        {
            return P1;
        }
        else if (PlayerAttack1 == Attack.Scissor && PlayerAttack2 == Attack.Rock)
        {
            return P1;
        }
        else if (PlayerAttack1 == Attack.Scissor && PlayerAttack2 == Attack.Paper)
        {
            return P2;
        }

        return null;
    }

    private CardPlayer GetWinner()
    {
        if (P1.Health == 0)
        {
            return P2;
        }
        else if (P2.Health == 0)
        {
            return P1;
        }
        return null;
    }

    public void Replay (string GamePlay) {
        SceneManager.LoadScene(GamePlay);
        Debug.Log("Ini Scene Main Menu Aktif" + GamePlay);
    }

    public void BackToHome (string MainMenu) {
        SceneManager.LoadScene(MainMenu);
        Debug.Log("Ini Scene Main Menu Aktif" + MainMenu);
    }
}