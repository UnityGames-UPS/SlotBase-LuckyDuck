using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using Newtonsoft.Json;
using System.ComponentModel;

public class SlotBehaviour : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField]
    private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    //class to store total images
    [SerializeField]
    private List<SlotImage> slotMatrix;     //class to store the result matrix
    [SerializeField] private Image[] extraIcons;

    [Header("Slots Transforms")]
    [SerializeField]
    private Transform[] Slot_Transform;

    [Header("Buttons")]
    [SerializeField]
    private CustomBtn SlotStart_Button;
    [SerializeField] private Button Turbo_Button;
    [SerializeField] private Button StopSpin_Button;
    // [SerializeField]
    // private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Sprite TurboToggleSprite;
    [SerializeField]
    private Button MaxBet_Button;
    [SerializeField]
    private Button BetPlus_Button;
    [SerializeField]
    private Button BetMinus_Button;

    [Header("Animated Sprites")]

    [SerializeField] private Sprite[] ID_0;
    [SerializeField] private Sprite[] ID_1;
    [SerializeField] private Sprite[] ID_2;
    [SerializeField] private Sprite[] ID_3;
    [SerializeField] private Sprite[] ID_4;
    [SerializeField] private Sprite[] ID_5;
    [SerializeField] private Sprite[] ID_6;
    [SerializeField] private Sprite[] ID_7;
    [SerializeField] private Sprite[] ID_8;
    [SerializeField] private Sprite[] ID_9;

    [Header("Miscellaneous UI")]
    [SerializeField]
    private TMP_Text Balance_text;
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text LineBet_text;
    [SerializeField]
    private TMP_Text TotalWin_text;


    [Header("Audio Management")]
    [SerializeField]
    private AudioController audioController;

    [SerializeField]
    private UIManager uiManager;



    [Header("Free Spins Board")]
    [SerializeField]
    private GameObject FSBoard_Object;
    [SerializeField]
    private TMP_Text FSnum_text;

    [SerializeField] private Sprite freeSpinReel;
    [SerializeField] private Sprite normalReel;
    [SerializeField] private Image reelBG;

    [SerializeField] private ImageAnimation[] ringAnim;

    int tweenHeight = 0;  //calculate the height at which tweening is done

    [SerializeField]
    private PayoutCalculation PayCalculator;

    private List<Tweener> alltweens = new List<Tweener>();

    private Tweener WinTween = null;


    [SerializeField]
    private SocketIOManager SocketManager;

    private Coroutine AutoSpinRoutine = null;
    private Coroutine FreeSpinRoutine = null;
    private Coroutine tweenroutine;
    private Coroutine winAnimRoutine;
    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    private bool IsSpinning = false;
    private bool CheckSpinAudio = false;
    internal bool CheckPopups = false;

    internal int BetCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    protected int Lines = 9;
    [SerializeField] private int IconSizeFactor = 100;
    private int numberOfSlots = 5;
    private bool IsTurboOn;
    private bool WasAutoSpinOn;
    private bool StopSpinToggle;
    private float SpinDelay = 0.2f;
    [SerializeField] private GameObject[] normalWinBlinkObj;
    [SerializeField] private GameObject[] freeSpinBlinkObj;

    internal int totalFreeSpins;
    private Tween BalanceTween;
    internal List<int> dynamicLinesIndex = new List<int>();
    private void Start()
    {
        IsAutoSpin = false;

        if (SlotStart_Button) SlotStart_Button.SpinAction = () => StartSlots();
        if (SlotStart_Button) SlotStart_Button.AutoSpinACtion = AutoSpin;
        // if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (BetPlus_Button) BetPlus_Button.onClick.RemoveAllListeners();
        if (BetPlus_Button) BetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });
        if (BetMinus_Button) BetMinus_Button.onClick.RemoveAllListeners();
        if (BetMinus_Button) BetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
        if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);

        if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
        if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });


        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

        if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
        if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

        if (FSBoard_Object) FSBoard_Object.SetActive(false);

        tweenHeight = (15 * IconSizeFactor) - 280;
        shuffleInitialMatrix();
    }


    void TurboToggle()
    {
        audioController.PlayButtonAudio();
        if (IsTurboOn)
        {
            IsTurboOn = false;
            Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
            Turbo_Button.image.sprite = TurboToggleSprite;
            Turbo_Button.image.color = new Color(0.86f, 0.86f, 0.86f, 1);
        }
        else
        {
            IsTurboOn = true;
            Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
            Turbo_Button.image.color = new Color(1, 1, 1, 1);
        }
    }
    #region Autospin
    private void AutoSpin()
    {
        if (!IsAutoSpin)
        {

            IsAutoSpin = true;
            if (!AutoSpinStop_Button.interactable) AutoSpinStop_Button.interactable = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (SlotStart_Button) SlotStart_Button.gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());

        }
    }

    private void StopAutoSpin()
    {
        if (IsAutoSpin)
        {
            IsAutoSpin = false;
            WasAutoSpinOn = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (SlotStart_Button) SlotStart_Button.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
        }
        WasAutoSpinOn = false;
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }
    #endregion

    #region FreeSpin
    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {
            if (FSnum_text) FSnum_text.text = $"{spins}/{totalFreeSpins}";
            if (FSBoard_Object) FSBoard_Object.SetActive(true);
            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;
        InvokeRepeating(nameof(FreeSpinBlinkAnim), 0.2f, 0.15f);
        reelBG.sprite = freeSpinReel;
        while (i < spinchances)
        {
            i++;
            uiManager.FreeSpins--;
            if (FSnum_text) FSnum_text.text = $"{uiManager.FreeSpins}/{totalFreeSpins}";
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
        }
        reelBG.sprite = normalReel;
        totalFreeSpins = 0;
        uiManager.FreeSpins = 0;
        CancelInvoke("FreeSpinBlinkAnim");
        if (FSBoard_Object) FSBoard_Object.SetActive(false);
        if (WasAutoSpinOn)
        {
            AutoSpin();
        }
        else
        {
            ToggleButtonGrp(true);
        }
        IsFreeSpin = false;
        if (!AutoSpinStop_Button.interactable)
            AutoSpinStop_Button.interactable = true;

    }
    #endregion

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
        }
    }



    private void MaxBet()
    {
        if (audioController) audioController.PlayButtonAudio();
        BetCounter = SocketManager.initialData.bets.Count - 1;
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.bets[BetCounter] * Lines).ToString();
        currentTotalBet = SocketManager.initialData.bets[BetCounter] * Lines;
        // CompareBalance();
    }

    private void ChangeBet(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (IncDec)
        {
            BetCounter++;
            if (BetCounter > SocketManager.initialData.bets.Count - 1)
            {
                BetCounter = 0;
            }
        }
        else
        {
            BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter = SocketManager.initialData.bets.Count - 1;
            }
        }
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.bets[BetCounter] * Lines).ToString();
        currentTotalBet = SocketManager.initialData.bets[BetCounter] * Lines;
        uiManager.InitialiseUIData(SocketManager.initUIData.paylines);
        // CompareBalance();
    }

    #region InitialFunctions
    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < slotMatrix.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length);
                slotMatrix[i].slotImages[j].rendererDelegate.sprite = myImages[randomIndex];
            }
        }
        shuffleExtraIcons();
    }

    internal void shuffleExtraIcons()
    {

        for (int i = 0; i < extraIcons.Length; i++)
        {
            extraIcons[i].sprite = myImages[UnityEngine.Random.Range(0, myImages.Length)];
        }
    }
    internal void SetInitialUI()
    {
        BetCounter = 0;
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.bets[BetCounter] * Lines).ToString();
        if (TotalWin_text) TotalWin_text.text = "0.00";
        if (Balance_text) Balance_text.text = SocketManager.playerdata.balance.ToString("f2");
        currentBalance = SocketManager.playerdata.balance;
        currentTotalBet = SocketManager.initialData.bets[BetCounter] * Lines;
        PayCalculator.paylines.AddRange(SocketManager.initialData.lines);
        // _bonusManager.PopulateWheel(SocketManager.bonusdata);
        CompareBalance();
        Debug.Log(JsonConvert.SerializeObject(SocketManager.initialData.lines));
        uiManager.InitialiseUIData(SocketManager.initUIData.paylines);
    }
    #endregion

    private void OnApplicationFocus(bool focus)
    {
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }

    //function to populate animation sprites accordingly
    private void PopulateAnimationSprites(ImageAnimation animScript, int val)
    {
        animScript.textureArray.Clear();
        switch (val)
        {


            case 0:
                animScript.textureArray.AddRange(ID_0);
                animScript.AnimationSpeed = ID_0.Length - 10;
                break;
            case 1:
                animScript.textureArray.AddRange(ID_1);
                animScript.AnimationSpeed = ID_1.Length - 10;
                break;
            case 2:
                animScript.textureArray.AddRange(ID_2);
                animScript.AnimationSpeed = ID_2.Length - 10;
                break;
            case 3:
                animScript.textureArray.AddRange(ID_3);
                animScript.AnimationSpeed = ID_3.Length - 10;
                break;
            case 4:
                animScript.textureArray.AddRange(ID_4);
                animScript.AnimationSpeed = ID_4.Length - 10;
                break;
            case 5:
                animScript.textureArray.AddRange(ID_5);
                animScript.AnimationSpeed = ID_5.Length - 10;
                break;
            case 6:
                animScript.textureArray.AddRange(ID_6);
                animScript.AnimationSpeed = ID_6.Length - 10;
                break;
            case 7:
                animScript.textureArray.AddRange(ID_7);
                animScript.AnimationSpeed = ID_7.Length - 10;
                break;
            case 8:
                animScript.textureArray.AddRange(ID_8);
                animScript.AnimationSpeed = ID_8.Length - 10;
                break;
            case 9:
                animScript.textureArray.AddRange(ID_9);
                animScript.AnimationSpeed = ID_9.Length - 10;
                break;

        }
        // if (animScript.AnimationSpeed <= 16)
        //     animScript.AnimationSpeed = 7.5f;
    }

    #region SlotSpin
    //starts the spin process
    private void StartSlots(bool autoSpin = false)
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }
        }
        WinningsAnim(false);
        if (winAnimRoutine != null)
            StopCoroutine(winAnimRoutine);



        if (SlotStart_Button) SlotStart_Button.btn.interactable = false;
        // if (TempList.Count > 0)
        // {
        StopGameAnimation();
        // }
        PayCalculator.ResetLines(true);
        tweenroutine = StartCoroutine(TweenRoutine());
    }

    //manage the Routine for spinning of the slots
    private IEnumerator TweenRoutine()
    {
        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            ToggleButtonGrp(true);
            yield break;
        }
        PayCalculator.DontDestroy.Clear();
        if (audioController) audioController.PlayWLAudio("spin");
        if (TotalWin_text) TotalWin_text.text = "0.00";
        CheckSpinAudio = true;

        IsSpinning = true;

        ToggleButtonGrp(false);
        if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
        {
            StopSpin_Button.gameObject.SetActive(true);
        }
        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.1f);
        }

        if (!IsFreeSpin)
        {
            BalanceDeduction();
        }
        SocketManager.AccumulateResult(BetCounter);

        yield return new WaitUntil(() => SocketManager.isResultdone);

        List<int> ringIndex = new List<int>();
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 5; i++)
            {

                int id = Int32.Parse(SocketManager.resultData.matrix[j][i]);
                if (id == 8 && i < SocketManager.resultData.matrix[j].Count)
                {

                    if (!ringIndex.Contains(i + 1))
                        ringIndex.Add(i + 1);
                }

                slotMatrix[i].slotImages[j].rendererDelegate.sprite = myImages[id];
                PopulateAnimationSprites(slotMatrix[i].slotImages[j], id);
            }
        }

        if (IsTurboOn|| IsFreeSpin )
        {

            yield return new WaitForSeconds(0.1f);
            StopSpinToggle = true;
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.1f);
                if (StopSpinToggle)
                {
                    break;
                }
            }
            StopSpin_Button.gameObject.SetActive(false);
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            if (i + 1 < numberOfSlots && ringIndex.Contains(i + 1) && !StopSpinToggle)
            {
                if (!IsTurboOn && !StopSpinToggle)
                    ActivateRing(i + 1);

                yield return StopTweening(5, Slot_Transform[i], i, true, StopSpinToggle, IsTurboOn);

            }
            else
                yield return StopTweening(5, Slot_Transform[i], i, false, StopSpinToggle, IsTurboOn);
            DeactivateRing();
        }

        StopSpinToggle = false;
        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();
        if (audioController) audioController.StopWLAaudio();
        if (SocketManager.playerdata.currentWining > 0)
        {
            SpinDelay = 2f;
        }
        else
        {
            SpinDelay = 1f;
        }
        // if (!IsAutoSpin && !IsFreeSpin)
        //     winAnimRoutine = StartCoroutine(ShowIconByPayline(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit));
        // else
        //     CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit);

        // CheckPopups = true;
        if (SocketManager.resultData.payload.winAmount > 0)
            TotalWin_text.text = $" Win\n{SocketManager.resultData.payload.winAmount.ToString("f3")}";
        else if (SocketManager.resultData.freeSpin.isFreeSpin)
            TotalWin_text.text = $"Win\n{SocketManager.resultData.freeSpin.count} Free Spins";
        else
            TotalWin_text.text = $"Better Luck Next Time";
        BalanceTween?.Kill();
        if (Balance_text) Balance_text.text = SocketManager.playerdata.balance.ToString("f3");
        if (SocketManager.resultData.payload.winAmount > 0)
        {
            List<int> winLine = new();
            foreach (var win in SocketManager.resultData.payload.wins)
            {
                winLine.Add(win.line);
            }
            CheckPopups = true;
            // CheckPayoutLineBackend(winLine);
            StartCoroutine(CheckPayoutLineBackend(winLine));

            yield return new WaitUntil(() => !CheckPopups);
        }
        CheckForFeaturesAnimation();


        currentBalance = SocketManager.playerdata.balance;

        CheckWinPopups();

        yield return new WaitUntil(() => !CheckPopups);
        if (audioController) audioController.StopWLAaudio();
        if (!IsAutoSpin && !IsFreeSpin)
        {
            ToggleButtonGrp(true);
        }

        IsSpinning = false;

        if (SocketManager.resultData.freeSpin.isFreeSpin)
        {
            if (IsFreeSpin)
            {
                IsFreeSpin = false;
                if (FreeSpinRoutine != null)
                {
                    StopCoroutine(FreeSpinRoutine);
                    FreeSpinRoutine = null;
                }
                // totalFreeSpins+=(int)SocketManager.resultData.freeSpins.count;
            }
            uiManager.FreeSpinProcess((int)SocketManager.resultData.freeSpin.count);
            if (IsAutoSpin)
            {
                WasAutoSpinOn = true;
                IsAutoSpin = false;
                if (AutoSpinRoutine != null || tweenroutine != null)
                    StopCoroutine(AutoSpinRoutine);
                AutoSpinStop_Button.interactable = false;
                // if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
                // if (SlotStart_Button) SlotStart_Button.gameObject.SetActive(true);
                // StopAutoSpin();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void BalanceDeduction()
    {
        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f2");
        });
    }

    void ActivateRing(int index)
    {
        Debug.Log("index" + index);
        ringAnim[index].gameObject.SetActive(true);

    }

    void DeactivateRing()
    {
        for (int i = 1; i < ringAnim.Length; i++)
        {
            ringAnim[i].gameObject.SetActive(false);
        }
    }
    internal void CheckWinPopups()
    {
        if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 5 && SocketManager.resultData.payload.winAmount < currentTotalBet * 10)
        {
            uiManager.PopulateWin(1, SocketManager.resultData.payload.winAmount);
        }
        else if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 10 && SocketManager.resultData.payload.winAmount < currentTotalBet * 15)
        {
            uiManager.PopulateWin(2, SocketManager.resultData.payload.winAmount);
        }
        else if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 15)
        {
            uiManager.PopulateWin(3, SocketManager.resultData.payload.winAmount);
        }
        else
        {
            CheckPopups = false;
        }
    }

    IEnumerator ShowIconByPayline(List<int> LineId, List<string> points_AnimString)
    {
        if (LineId.Count == 0 && points_AnimString.Count == 0)
            yield break;

        if (audioController) audioController.PlayWLAudio("win");
        List<List<string>> coordToAnimate = new List<List<string>>();

        for (int i = 0; i < LineId.Count; i++)
        {
            List<string> iconPerPayline = new List<string>();
            for (int j = 0; j < PayCalculator.paylines[LineId[i]].Count; j++)
            {
                if (points_AnimString.Contains($"{j},{PayCalculator.paylines[LineId[i]][j]}"))
                {
                    iconPerPayline.Add($"{j},{PayCalculator.paylines[LineId[i]][j]}");
                }

            }

            coordToAnimate.Add(iconPerPayline); ;
        }
        List<int> points = new List<int>();
        List<ImageAnimation> animatingIcon = new List<ImageAnimation>();
        WinningsAnim(true);
        while (true)
        {

            for (int i = 0; i < coordToAnimate.Count; i++)
            {

                PayCalculator.GeneratePayoutLines(LineId[i], true);
                for (int j = 0; j < coordToAnimate[i].Count; j++)
                {
                    points = coordToAnimate[i][j]?.Split(',')?.Select(Int32.Parse)?.ToList();
                    slotMatrix[points[0]].slotImages[points[1]].StartAnimation();
                    animatingIcon.Add(slotMatrix[points[0]].slotImages[points[1]]);

                }
                yield return new WaitForSeconds(2f);
                PayCalculator.ResetLines(true);
                for (int j = 0; j < animatingIcon.Count; j++)
                {


                    animatingIcon[j].StopAnimation();

                }
                animatingIcon.Clear();
                points.Clear();
                // yield return new WaitForSeconds(0.5f);

            }

            yield return null;
        }

    }
    // private void CheckPayoutLineBackend(List<int> LineId, List<string> points_AnimString)
    // {
    //     List<int> points_anim = null;
    //     if (LineId.Count > 0 || points_AnimString.Count > 0)
    //     {

    //         if (audioController) audioController.PlayWLAudio("win");


    //         for (int i = 0; i < LineId.Count; i++)
    //         {
    //             PayCalculator.GeneratePayoutLines(LineId[i], true);
    //         }

    //         for (int i = 0; i < points_AnimString.Count; i++)
    //         {
    //             points_anim = points_AnimString[i]?.Split(',')?.Select(Int32.Parse)?.ToList();
    //             slotMatrix[points_anim[0]].slotImages[points_anim[1]].StartAnimation();
    //         }

    //         WinningsAnim(true);
    //     }

    //     CheckSpinAudio = false;
    // }

    private IEnumerator CheckPayoutLineBackend(List<int> LineId)
    {
        float delay = 0f;
        if (IsFreeSpin || IsTurboOn) delay = 0.3f;
        else delay = 1f;
        if (LineId.Count > 0)
        {
            List<KeyValuePair<int, int>> Totalcoords = new();
            for (int i = 0; i < LineId.Count; i++)
            {
                List<KeyValuePair<int, int>> coords = new();

                Debug.Log("line come " + LineId[i]);
                PayCalculator.GeneratePayoutLines(LineId[i], true);
                dynamicLinesIndex.Add(LineId[i]);
                for (int k = 0; k < SocketManager.resultData.payload.wins[i].positions.Count; k++)
                {
                    int rowIndex = SocketManager.initialData.lines[LineId[i]][SocketManager.resultData.payload.wins[i].positions[k]];
                    int columnIndex = SocketManager.resultData.payload.wins[i].positions[k];
                    coords.Add(new KeyValuePair<int, int>(rowIndex, columnIndex));
                    Totalcoords.Add(new KeyValuePair<int, int>(rowIndex, columnIndex));
                }

                foreach (var coord in coords)
                {
                    int rowIndex = coord.Key;
                    int columnIndex = coord.Value;
                    slotMatrix[columnIndex].slotImages[rowIndex].StartAnimation();
                }
                yield return new WaitForSeconds(delay);
                foreach (var coord in coords)
                {

                    int rowIndex = coord.Key;
                    int columnIndex = coord.Value;
                    slotMatrix[columnIndex].slotImages[rowIndex].StopAnimation();
                    //Tempimages[columnIndex].slotImages[rowIndex].gameObject.GetComponent<ImageAnimation>().StopAnimation();
                }
                // PayoutLines[LineId[i]].SetActive(false);
                PayCalculator.ResetLines(true);
            }
            for (int i = 0; i < LineId.Count; i++)
            {
                PayCalculator.GeneratePayoutLines(LineId[i], true);
                foreach (var coord in Totalcoords)
                {
                    int rowIndex = coord.Key;
                    int columnIndex = coord.Value;
                    // StartGameAnimation(Tempimages[columnIndex].slotImages[rowIndex].gameObject);
                    // ReelsHideGameObject1[columnIndex].slotImages[rowIndex].gameObject.SetActive(false);
                    // ReelsFrameGameObject1[columnIndex].slotImages[rowIndex].gameObject.SetActive(true);
                    slotMatrix[columnIndex].slotImages[rowIndex].StartAnimation();
                }

            }

            WinningsAnim(true);
            CheckPopups = false;
        }
        else
        {
            //if (audioController) audioController.PlayWLAudio("lose");
            if (audioController) audioController.StopWLAaudio();
        }
        CheckSpinAudio = false;
    }

    private void CheckForFeaturesAnimation()
    {
        bool playScatter = false;
        bool playBonus = false;
        bool playFreespin = false;
        // if (SocketManager.resultData.scatter.amount > 0)
        // {
        //     playScatter = true;
        // }
        // if (SocketManager.resultData.bonus.istriggered)
        // {
        //     playBonus = true;
        // }
        if (SocketManager.resultData.freeSpin.isFreeSpin)
        {
            playFreespin = true;
        }
        PlayFeatureAnimation(playScatter, playBonus, playFreespin);
    }
    private void PlayFeatureAnimation(bool scatter = false, bool bonus = false, bool freeSpin = false)
    {
        for (int i = 0; i < SocketManager.resultData.matrix.Count; i++)
        {
            for (int j = 0; j < SocketManager.resultData.matrix[i].Count; j++)
            {

                if (int.TryParse(SocketManager.resultData.matrix[i][j], out int parsedNumber))
                {
                    // if (scatter && parsedNumber == 12)
                    // {
                    //     StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
                    // }
                    // if (bonus && parsedNumber == 9)
                    // {
                    //     StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
                    // }
                    if (freeSpin && parsedNumber == 8)
                    {
                        slotMatrix[j].slotImages[i].StartAnimation();
                        // StartGameAnimation(Tempimages[j].slotImages[i].transform);
                    }
                }

            }
        }
    }




    private void WinningsAnim(bool IsStart)
    {
        if (IsStart)
        {
            WinTween = TotalWin_text.transform.DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
            //  InvokeRepeating("BlinkAnim", 0.2f, 0.25f);
        }
        else
        {
            // CancelInvoke("BlinkAnim");
            WinTween.Kill();
            TotalWin_text.transform.localScale = Vector3.one;
        }
    }

    #endregion

    internal void CallCloseSocket()
    {
       StartCoroutine(SocketManager.CloseSocket());
    }

    void BlinkAnim()
    {

        if (normalWinBlinkObj[0].activeSelf)
        {
            normalWinBlinkObj[0].SetActive(false);
            normalWinBlinkObj[1].SetActive(true);
        }
        else
        {
            normalWinBlinkObj[1].SetActive(false);
            normalWinBlinkObj[0].SetActive(true);
        }
    }
    void FreeSpinBlinkAnim()
    {

        if (freeSpinBlinkObj[0].activeSelf)
        {
            freeSpinBlinkObj[0].SetActive(false);
            freeSpinBlinkObj[1].SetActive(true);
        }
        else
        {
            freeSpinBlinkObj[1].SetActive(false);
            freeSpinBlinkObj[0].SetActive(true);
        }
    }

    void ToggleButtonGrp(bool toggle)
    {

        if (SlotStart_Button) SlotStart_Button.btn.interactable = toggle;
        if (MaxBet_Button) MaxBet_Button.interactable = toggle;
        // if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
        if (BetMinus_Button) BetMinus_Button.interactable = toggle;
        if (BetPlus_Button) BetPlus_Button.interactable = toggle;

    }

    //start the icons animation


    //stop the icons animation
    private void StopGameAnimation()
    {
        for (int i = 0; i < slotMatrix.Count; i++)
        {
            for (int j = 0; j < slotMatrix[i].slotImages.Count; j++)
            {
                slotMatrix[i].slotImages[j].StopAnimation();
                slotMatrix[i].slotImages[j].textureArray.Clear();
            }
        }
    }


    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        tweener.Play();
        alltweens.Add(tweener);
    }



    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool delay = false, bool isStop = false, bool turbo = false)
    {
        alltweens[index].Pause();
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        alltweens[index] = slotTransform.DOLocalMoveY(-860, 0.2f).SetEase(Ease.OutElastic);


        if (!isStop)
        {
            if (delay && !turbo)
                yield return new WaitForSeconds(1f);
            else
                yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return null;
        }
    }


    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<ImageAnimation> slotImages = new List<ImageAnimation>(10);
}

