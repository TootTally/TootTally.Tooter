using BaboonAPI.Hooks.Tracks;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TootTally.Graphics;
using TootTally.Graphics.Animation;
using TootTally.Utils;
using TootTally.Utils.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TootTally.Tooter
{
    public static class TooterManager
    {
        private static ScoreData _scoreData;
        private static DemonDialogue _currentDemonDialogueInstance;
        private static CardSceneController _currentCardSceneInstance;
        private static RectTransform _tooterButtonOutlineRectTransform;
        private static bool _isSceneActive;
        private static CustomAnimation _tooterBtnAnimation, _tooterTextAnimation;
        private static bool _tooterButtonLoaded;
        private static bool _tooterButtonClicked;
        private static GameObject _characterPefab;
        private static GameObject _soda, _appaloosa, _beezerly, _kaizyle, _trixiebell;
        private static SpriteRenderer _sodaSprite, _appaloosaSprite, _beezerlySprite, _kaizyleSprite, _trixiebellSprite;
        private static PopUpNotif _txtBox;
        private static int _currentDialogueState;
        private static readonly Vector2 _btn2PositionRight = new Vector2(305, -180);
        private static readonly Vector2 _btn2PositionCenter = new Vector2(65, -190);

        private static readonly Vector3 _leftCharPosition = new Vector3(-8f, -6.5f, 10);
        private static readonly Vector3 _leftCenterCharPosition = new Vector3(-4.75f, -6.5f, 10);
        private static readonly Vector3 _centerCharPosition = new Vector3(-1.5f, -6.5f, 10);
        private static readonly Vector3 _rightCenterCharPosition = new Vector3(1f, -6.5f, 10);
        private static readonly Vector3 _rightCharPosition = new Vector3(4f, -6.5f, 10);
        private static readonly Vector3 _farRightCharPosition = new Vector3(6.8f, -6.5f, 10);
        private static readonly Vector3 _outLeftCharPosition = new Vector3(-15, -6.5f, 10);
        private static readonly Vector3 _outRightCharPosition = new Vector3(15, -6.5f, 10);
        private static readonly string _brokenWindow = "<color='#007ACC'>CRASH</color>";
        private static readonly string _sodaColoredName = "<color='#FFFF21'>Soda</color>";
        private static readonly string _trixieColoredName = "<color='#FFAAAA'>Trixiebell</color>";
        private static readonly string _appaloosaColoredName = "<color='#FF0000'>Appaloosa</color>";
        private static readonly string _beezerlyColoredName = "<color='#f0f0c2'>Beezerly</color>";
        private static readonly string _kaizyleColoredName = "<color='#A020F0'>Kaizyle</color>";
        private static readonly string _tromBurgerChampName = "<color='#D62300'>TromBurger</color> " + "<color='#FF8732'>Champ</color>";
        private static List<Coroutine> _textCoroutines = new List<Coroutine>();
        private static readonly string _loveHasNoEndTrackref = "0.8506432151619188";
        private static readonly string _loveFlipTrackref = "0.46110644885682883";
        private static readonly string _letBeYourselfTrackref = "0.1329585353620475";
        private static readonly string _lateNightJazTrackref = "0.03104072181033679";
        private static readonly string _memoriesOfYouTrackref = "0.9615501183653947";
        private static readonly string _pathOfDiscoveriesTrackref = "0.9448386137778275";
        private static bool _completedGame;

        public static void OnModuleLoad()
        {
            _tooterButtonLoaded = false;
            _dialogueStates = GetDialogueChapter1And2();
        }

        private static IEnumerator addWord(DemonDialogue __instance, string word, float delayTime)
        {
            float seconds = delayTime * 0.035f;
            yield return new WaitForSeconds(seconds);
            _txtBox.UpdateText(_txtBox.GetText + word + " ");
            _textCoroutines.RemoveAt(0);
            if (_textCoroutines.Count <= 0)
            {
                AnimationManager.AddNewScaleAnimation(__instance.btn1obj, new Vector3(1, 1, 0), .45f, new EasingHelper.SecondOrderDynamics(4.25f, .8f, 1.2f));
                AnimationManager.AddNewScaleAnimation(__instance.btn2obj, new Vector3(1, 1, 0), .45f, new EasingHelper.SecondOrderDynamics(4.25f, .8f, 1.2f), delegate { __instance.readytoclick = true; });
            }
            yield break;
        }

        //Pulled from DNSpy Token: 0x060001BA RID: 442 RVA: 0x0001C55C File Offset: 0x0001A75C
        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.dtxt))]
        [HarmonyPrefix]
        private static bool OnDTxtPostFixSetCoroutine(DemonDialogue __instance, object[] __args)
        {
            _textCoroutines.ForEach(c => Plugin.Instance.StopCoroutine(c));
            _textCoroutines.Clear();
            var dtext = (string)__args[0];
            string[] array = dtext.Split(new char[]
            {
            ' '
            });
            float delay = 0f;
            foreach (string word in array)
            {
                _textCoroutines.Add(Plugin.Instance.StartCoroutine(addWord(__instance, word, delay)));
                delay += 1.5f;
            }
            return false;
        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
        [HarmonyPrefix]
        public static bool OnPointSceneControllerCompleteGame()
        {
            if (_isSceneActive)
            {
                _isSceneActive = false;
                _completedGame = true;
                SceneManager.LoadScene("home");
                return false;
            }
            return true;
        }
        

        [HarmonyPatch(typeof(Plugin), nameof(Plugin.Update))]
        [HarmonyPostfix]
        public static void KeyPressDetectionOnUpdate()
        {
            if (_isSceneActive)
            {
                if (Input.GetKeyDown(KeyCode.K) && Input.GetKeyDown(KeyCode.I) && Input.GetKeyDown(KeyCode.S))
                {
                    PopUpNotifManager.DisplayNotif("A Secret KISS Was Found", Color.cyan);
                    DialogueFlags.kissedSomeone = false;
                }

                if (Input.GetKeyDown(KeyCode.Space) && _currentDemonDialogueInstance.readytoclick)
                {
                    if (_dialogueStates[_currentDialogueState].option1Text != "")
                        OverwriteClickBtn1(_currentDemonDialogueInstance);
                    else if (_dialogueStates[_currentDialogueState].option2Text != "")
                        OverwriteClickBtn2(_currentDemonDialogueInstance);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad1) && _dialogueStates[_currentDialogueState].option1Text != "" && _currentDemonDialogueInstance.readytoclick)
                    OverwriteClickBtn1(_currentDemonDialogueInstance);
                else if (Input.GetKeyDown(KeyCode.Keypad2) && _dialogueStates[_currentDialogueState].option2Text != "" && _currentDemonDialogueInstance.readytoclick)
                    OverwriteClickBtn2(_currentDemonDialogueInstance);
            }
        }

        [HarmonyPatch(typeof(BabbleController), nameof(BabbleController.doBabbles))]
        [HarmonyPrefix]
        public static void OnDemonDialogueAddWordPostFix(BabbleController __instance)
        {
            __instance.babbleplayer.mute = true; //there's some coding logic inside of the doBabble function which I can't overwrite... so just mute the babble and let it do its things
        }

        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.Start))]
        [HarmonyPostfix]
        public static void OnDemonDiablogueStart(DemonDialogue __instance)
        {


            if (_currentDialogueState != 0) return;
            _currentDialogueState = -1;
            _currentDemonDialogueInstance = __instance;
            _isSceneActive = true;
            GlobalVariables.chosen_character = 7;
            GlobalVariables.chosen_trombone = 0;
            GlobalVariables.chosen_soundset = 0;
            GlobalVariables.show_toot_rainbow = false;
            GlobalVariables.gamescrollspeed = 1f;
            GlobalVariables.levelselect_index = 0;
            __instance.btn1obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-65, -190);
            __instance.btn2obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(65, -180);
            __instance.txtbox.gameObject.SetActive(false);
            SetCharacterPrefab();
            _soda = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Soda", _outLeftCharPosition, TooterAssetsManager.GetSprite(ExpressionToSpritePath(CharExpressions.SodaNeutral)));
            _sodaSprite = _soda.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _appaloosa = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Appaloosa", _outRightCharPosition, TooterAssetsManager.GetSprite(ExpressionToSpritePath(CharExpressions.AppaloosaNeutral)));
            _appaloosaSprite = _appaloosa.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _beezerly = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Beezerly", _outRightCharPosition, TooterAssetsManager.GetSprite(ExpressionToSpritePath(CharExpressions.BeezerlyNeutral)));
            _beezerlySprite = _beezerly.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _kaizyle = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Kaizyle", _outRightCharPosition, TooterAssetsManager.GetSprite(ExpressionToSpritePath(CharExpressions.KaizyleNeutral)));
            _kaizyleSprite = _kaizyle.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _trixiebell = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Trixiebell", _outRightCharPosition, TooterAssetsManager.GetSprite(ExpressionToSpritePath(CharExpressions.TrixieNeutral)));
            _trixiebellSprite = _trixiebell.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _txtBox = GameObjectFactory.CreateNotif(GameObject.Find("CAM_middletier/DemonCanvas/Panel").transform, "NovelTextBox", "", GameTheme.themeColors.notification.defaultText);
            RectTransform txtBoxRectTransform = _txtBox.GetComponent<RectTransform>();
            _txtBox.transform.SetSiblingIndex(1);
            txtBoxRectTransform.anchoredPosition = new Vector2(0, -300);
            txtBoxRectTransform.sizeDelta = new Vector2(1500, 250);
            txtBoxRectTransform.localScale = Vector2.one / 2f;
            _txtBox.Initialize(float.MaxValue, new Vector2(0, -150), new Vector2(1000, 250), new Vector2(300, 0));
            _txtBox.SetTextSize(32); //SetTextSize has to be called after init
            _txtBox.SetTextAlign(TextAnchor.MiddleLeft);
            _txtBox.transform.Find("NotifText").GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
            __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
            __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(.6f, .6f, .6f);
            __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().preserveAspect = true;
            __instance.csc.demonbg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            __instance.csc.demonbg.GetComponent<RectTransform>().localScale = Vector2.one * 8f;
            GameObject.DestroyImmediate(__instance.csc.demonbg.transform.Find("Image (1)").gameObject);

            Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("Chapter1-2Music.mp3", clip =>
            {
                __instance.csc.bgmus2.clip = clip;
                __instance.csc.bgmus2.volume = .3f;
                __instance.csc.bgmus2.Play();
            }));
            Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("Chapter3Music.mp3", clip =>
            {
                __instance.csc.bgmus1.clip = clip;
                __instance.csc.bgmus1.volume = 0f;
            }));
            _scoreData = new ScoreData();

            var testDialogues = GetDialogueChapter1And2();
            Plugin.Instance.LogInfo("Dialogues 1 and 2 tested");
            testDialogues = GetDialogueChapter3();
            Plugin.Instance.LogInfo("Dialogues 3 tested");
            testDialogues = GetDialogueChapter4();
            Plugin.Instance.LogInfo("Dialogues 4 tested");
        }

        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.clickbtn1))]
        [HarmonyPrefix]
        public static bool OverwriteClickBtn1(DemonDialogue __instance)
        {
            if (__instance.readytoclick)
            {
                __instance.readytoclick = false;
                _scoreData.AddScore(_dialogueStates[_currentDialogueState].option1Score);
                __instance.doDialogue(__instance.btnyeschoice);
            }
            return false;
        }

        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.clickbtn2))]
        [HarmonyPrefix]
        public static bool OverwriteClickBtn2(DemonDialogue __instance)
        {
            if (__instance.readytoclick)
            {
                __instance.readytoclick = false;
                _scoreData.AddScore(_dialogueStates[_currentDialogueState].option2Score);
                __instance.doDialogue(__instance.btnnochoice);
            }
            return false;
        }

        public static void FlipSpriteAnimation(GameObject character, bool fixPosition, float speedMult = 1f)
        {
            var scaleX = character.transform.localScale.x;
            var scaleY = character.transform.localScale.y;
            AnimationManager.AddNewTransformScaleAnimation(character, new Vector3(-scaleX, scaleY, 10f), 1.8f, new EasingHelper.SecondOrderDynamics(1.25f * speedMult, 1f, 0f), delegate { character.transform.localScale = new Vector2(Mathf.Sign(-scaleX) * 1.6f, 1.6f); });
            if (fixPosition)
                AnimationManager.AddNewTransformPositionAnimation(character, character.transform.position + new Vector3(Mathf.Sign(scaleX) * 1.1f, 0, 0), 1.8f, GetSecondDegreeAnimationFunction(speedMult));

        }
        public static void FlipSpriteAnimation(GameObject character, bool fixPosition, float timespan, float speedMult = 1f)
        {
            var scaleX = character.transform.localScale.x;
            var scaleY = character.transform.localScale.y;
            AnimationManager.AddNewTransformScaleAnimation(character, new Vector3(-scaleX, scaleY, 10f), timespan, new EasingHelper.SecondOrderDynamics(1.25f * speedMult, 1f, 0f), delegate { character.transform.localScale = new Vector2(Mathf.Sign(-scaleX) * 1.6f, 1.6f); });
            if (fixPosition)
                AnimationManager.AddNewTransformPositionAnimation(character, character.transform.position + new Vector3(Mathf.Sign(scaleX) * 1.1f, 0, 0), timespan, GetSecondDegreeAnimationFunction(speedMult));

        }

        public static void FlipSpriteRightAnimation(GameObject character, bool fixPosition, float speedMult = 1f)
        {
            var scaleX = character.transform.localScale.x;
            var scaleY = character.transform.localScale.y;
            AnimationManager.AddNewTransformScaleAnimation(character, new Vector3(-Math.Abs(scaleX), scaleY, 10f), 1.8f / (speedMult / 2f), new EasingHelper.SecondOrderDynamics(1.25f * speedMult, 1f, 0f), delegate { character.transform.localScale = new Vector2(Mathf.Sign(-Math.Abs(scaleX)) * 1.6f, 1.6f); });
            if (fixPosition)
                AnimationManager.AddNewTransformPositionAnimation(character, character.transform.position + new Vector3(Mathf.Sign(Math.Abs(scaleX)) * 1.1f, 0, 0), 1.8f, GetSecondDegreeAnimationFunction(speedMult));

        }

        public static void FlipSpriteLeftAnimation(GameObject character, bool fixPosition, float speedMult = 1f)
        {
            var scaleX = character.transform.localScale.x;
            var scaleY = character.transform.localScale.y;
            AnimationManager.AddNewTransformScaleAnimation(character, new Vector3(Math.Abs(scaleX), scaleY, 10f), 1.8f / (speedMult / 2f), new EasingHelper.SecondOrderDynamics(1.25f * speedMult, 1f, 0f), delegate { character.transform.localScale = new Vector2(Mathf.Sign(Math.Abs(scaleX)) * 1.6f, 1.6f); });
            if (fixPosition)
                AnimationManager.AddNewTransformPositionAnimation(character, character.transform.position + new Vector3(Mathf.Sign(Math.Abs(scaleX)) * -1.1f, 0, 0), 1.8f, GetSecondDegreeAnimationFunction(speedMult));

        }

        public static IEnumerator<UnityWebRequestAsyncOperation> TryLoadingAudioClipLocal(string fileName, Action<AudioClip> callback)
        {
            string assetDir = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "Assets");
            assetDir = Path.Combine(assetDir, fileName);
            UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + assetDir, AudioType.MPEG);
            yield return webRequest.SendWebRequest();
            callback(DownloadHandlerAudioClip.GetContent(webRequest));
        }

        public static void SetCharacterPrefab()
        {
            GameObject demon = GameObject.Find("CAM_demonpuppet/Demon");

            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/demon-eyebrow-thin").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/demon-def-eyes-default/demon-eyelids-bottom").gameObject);

            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/demon-faces").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/demon-def-tail").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/demon-def-eyes-default").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-feet").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/arms-excited").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/arms-def").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/arms-thinking").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/arms-idea").gameObject);
            GameObject.DestroyImmediate(demon.transform.Find("demon-def-body/demon-eyebrows-big").gameObject);
            _characterPefab = GameObject.Instantiate(demon);
            _characterPefab.transform.position = Vector2.zero;
            _characterPefab.transform.localScale = Vector2.one * 1.6f;
            _characterPefab.SetActive(false);
        }

        public static GameObject CreateCharacterFromPrefab(Transform canvasTransform, string name, Vector3 position, Sprite characterSprite)
        {
            GameObject character = GameObject.Instantiate(_characterPefab, canvasTransform);
            character.transform.position = position;
            character.GetComponent<Animator>().speed = 0.1f;
            character.name = name;
            character.transform.Find("demon-def-body").GetComponent<SpriteRenderer>().sprite = characterSprite;
            character.SetActive(true);

            return character;
        }


        [HarmonyPatch(typeof(CardSceneController), nameof(CardSceneController.Start))]
        [HarmonyPostfix]
        public static void OnCardSceneControllerEnter(CardSceneController __instance)
        {
            if (_currentCardSceneInstance != null) return;

            _currentCardSceneInstance = __instance;

            if (_tooterButtonClicked)
            {
                //__instance.fadeMus(0, false);
                __instance.bgmus1.Stop();
                __instance.bgmus2.Stop();
                __instance.demoncanvas.SetActive(true);
                __instance.demoncanvasgroup.alpha = 1f;

                GlobalVariables.localsave.progression_demon_sets = 7;
                GlobalVariables.localsave.lore_dump_demon = false;
                GlobalVariables.localsave.progression_trombone_champ = true;

                __instance.Invoke("startDemonDiag", 0f);
                __instance.killDemonCards();
            }
        }

        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.startDiag))]
        [HarmonyPrefix]
        public static void Test(object[] __args)
        {
            Plugin.Instance.LogInfo("start: " + (int)__args[0]);
        }

        public static void OverwriteGameObjectSpriteAndColor(GameObject gameObject, string spriteName, Color spriteColor)
        {
            gameObject.GetComponent<Image>().sprite = TooterAssetsManager.GetSprite(spriteName);
            gameObject.GetComponent<Image>().color = spriteColor;
        }

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        public static void OnHomeControllerStartPostFixAddTooterButton(HomeController __instance)
        {

            GameObject mainCanvas = GameObject.Find("MainCanvas").gameObject;
            GameObject mainMenu = mainCanvas.transform.Find("MainMenu").gameObject;

            #region TooterButton
            GameObject tooterButton = GameObject.Instantiate(__instance.btncontainers[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            GameObject tooterHitbox = GameObject.Instantiate(mainMenu.transform.Find("Button2").gameObject, mainMenu.transform);
            GameObject tooterText = GameObject.Instantiate(__instance.paneltxts[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform.Find("AllPanelText"));
            tooterButton.name = "TOOTERContainer";
            tooterHitbox.name = "TOOTERButton";
            tooterText.name = "TOOTERText";
            OverwriteGameObjectSpriteAndColor(tooterButton.transform.Find("FG").gameObject, "TooterButton.png", Color.white);
            OverwriteGameObjectSpriteAndColor(tooterText, "TooterText.png", Color.white);
            tooterButton.transform.SetSiblingIndex(0);
            RectTransform tooterTextRectTransform = tooterText.GetComponent<RectTransform>();
            tooterTextRectTransform.anchoredPosition = new Vector2(-800, -400);
            tooterTextRectTransform.sizeDelta = new Vector2(456, 89);

            _tooterButtonOutlineRectTransform = tooterButton.transform.Find("outline").GetComponent<RectTransform>();
            _tooterButtonClicked = false;
            tooterHitbox.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_completedGame)
                {
                    PopUpNotifManager.DisplayNotif("Please restart game to play again, sorry!", GameTheme.themeColors.notification.defaultText);
                    return;
                }

                __instance.addWaitForClick();
                __instance.playSfx(3);
                //Yoinked from DNSpy KEKW
                __instance.musobj.Stop();
                __instance.quickFlash(2);
                __instance.fadeAndLoadScene(6);
                _tooterButtonClicked = true;
            });

            EventTrigger tooterBtnEvents = tooterHitbox.GetComponent<EventTrigger>();
            tooterBtnEvents.triggers.Clear();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) =>
            {
                if (_tooterBtnAnimation != null)
                    _tooterBtnAnimation.Dispose();
                _tooterBtnAnimation = AnimationManager.AddNewScaleAnimation(tooterButton.transform.Find("outline").gameObject, new Vector2(1.01f, 1.01f), 0.5f, new EasingHelper.SecondOrderDynamics(3.75f, 0.80f, 1.05f));
                _tooterBtnAnimation.SetStartVector(_tooterButtonOutlineRectTransform.localScale);

                if (_tooterTextAnimation != null)
                    _tooterTextAnimation.Dispose();
                _tooterTextAnimation = AnimationManager.AddNewScaleAnimation(tooterText, new Vector2(1f, 1f), 0.5f, new EasingHelper.SecondOrderDynamics(3.5f, 0.65f, 1.15f));
                _tooterTextAnimation.SetStartVector(tooterText.GetComponent<RectTransform>().localScale);

                __instance.playSfx(2); // btn sound effect KEKW
                tooterButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(-2, 0);
                __instance.setDescText(0);
                __instance.desctext.text = "Choose your kiss carefully and take your time!";
                __instance.desctxtlimit = -1250;
            });
            tooterBtnEvents.triggers.Add(pointerEnterEvent);

            EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
            pointerExitEvent.eventID = EventTriggerType.PointerExit;
            pointerExitEvent.callback.AddListener((data) =>
            {
                if (_tooterBtnAnimation != null)
                    _tooterBtnAnimation.Dispose();
                _tooterBtnAnimation = AnimationManager.AddNewScaleAnimation(tooterButton.transform.Find("outline").gameObject, new Vector2(.4f, .4f), 0.5f, new EasingHelper.SecondOrderDynamics(1.50f, 0.80f, 1.00f));
                _tooterBtnAnimation.SetStartVector(_tooterButtonOutlineRectTransform.localScale);

                if (_tooterTextAnimation != null)
                    _tooterTextAnimation.Dispose();
                _tooterTextAnimation = AnimationManager.AddNewScaleAnimation(tooterText, new Vector2(.8f, .8f), 0.5f, new EasingHelper.SecondOrderDynamics(3.5f, 0.65f, 1.15f));
                _tooterTextAnimation.SetStartVector(tooterText.GetComponent<RectTransform>().localScale);

                tooterButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(2, 0);
                __instance.clearDescText();
            });

            tooterBtnEvents.triggers.Add(pointerExitEvent);
            _tooterButtonLoaded = true;
            #endregion

            #region graphics
            //Play and collect buttons are programmed differently... for some reasons
            GameObject collectBtnContainer = __instance.btncontainers[(int)HomeScreenButtonIndexes.Collect];
            GameThemeManager.OverwriteGameObjectSpriteAndColor(collectBtnContainer.transform.Find("FG").gameObject, "CollectButtonV2.png", Color.white);
            GameObject collectFG = collectBtnContainer.transform.Find("FG").gameObject;
            RectTransform collectFGRectTransform = collectFG.GetComponent<RectTransform>();
            collectBtnContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(900, 475.2f);
            collectBtnContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 190);
            collectFGRectTransform.sizeDelta = new Vector2(320, 190);
            GameObject collectOutline = __instance.allbtnoutlines[(int)HomeScreenButtonIndexes.Collect];
            GameThemeManager.OverwriteGameObjectSpriteAndColor(collectOutline, "CollectButtonOutline.png", Color.white);
            RectTransform collectOutlineRectTransform = collectOutline.GetComponent<RectTransform>();
            collectOutlineRectTransform.sizeDelta = new Vector2(351, 217.2f);
            GameObject textCollect = __instance.allpaneltxt.transform.Find("imgCOLLECT").gameObject;
            textCollect.GetComponent<RectTransform>().anchoredPosition = new Vector2(790, 430);
            textCollect.GetComponent<RectTransform>().sizeDelta = new Vector2(285, 48);
            textCollect.GetComponent<RectTransform>().pivot = Vector2.one / 2;

            GameObject improvBtnContainer = __instance.btncontainers[(int)HomeScreenButtonIndexes.Improv];
            GameObject improvFG = improvBtnContainer.transform.Find("FG").gameObject;
            RectTransform improvFGRectTransform = improvFG.GetComponent<RectTransform>();
            improvBtnContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 156);
            improvFGRectTransform.sizeDelta = new Vector2(450, 195);
            GameObject improvOutline = __instance.allbtnoutlines[(int)HomeScreenButtonIndexes.Improv];
            RectTransform improvOutlineRectTransform = improvOutline.GetComponent<RectTransform>();
            improvOutlineRectTransform.sizeDelta = new Vector2(470, 230);
            GameObject textImprov = __instance.allpaneltxt.transform.Find("imgImprov").gameObject;
            textImprov.GetComponent<RectTransform>().anchoredPosition = new Vector2(305, 385);
            textImprov.GetComponent<RectTransform>().sizeDelta = new Vector2(426, 54);
            #endregion

            #region hitboxes
            GameObject buttonCollect = mainMenu.transform.Find("Button2").gameObject;
            RectTransform buttonCollectTransform = buttonCollect.GetComponent<RectTransform>();
            buttonCollectTransform.anchoredPosition = new Vector2(739, 380);
            buttonCollectTransform.sizeDelta = new Vector2(320, 190);
            buttonCollectTransform.Rotate(0, 0, 15f);

            GameObject buttonImprov = mainMenu.transform.Find("Button4").gameObject;
            RectTransform buttonImprovTransform = buttonImprov.GetComponent<RectTransform>();
            buttonImprovTransform.anchoredPosition = new Vector2(310, 383);
            buttonImprovTransform.sizeDelta = new Vector2(450, 195);
            #endregion

        }

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Update))]
        [HarmonyPostfix]
        public static void AnimateTooterButton(HomeController __instance)
        {
            if (_tooterButtonLoaded)
                _tooterButtonOutlineRectTransform.transform.parent.transform.Find("FG/texholder").GetComponent<CanvasGroup>().alpha = (_tooterButtonOutlineRectTransform.localScale.y - 0.4f) / 1.5f;
        }

        public static void DisplayAchievement(string name, string description)
        {
            PopUpNotifManager.DisplayNotif($"Achievement: {name}\n{description}", GameTheme.themeColors.notification.defaultText);
        }

        public enum HomeScreenButtonIndexes
        {
            Play = 0,
            Collect = 1,
            Quit = 2,
            Improv = 3,
            Baboon = 4,
            Credit = 5,
            Settings = 6,
            Advanced = 7
        }
        public static EasingHelper.SecondOrderDynamics GetSecondDegreeAnimationFunction(float speedMult = 1f) => new EasingHelper.SecondOrderDynamics(1.15f * speedMult, 1f, 0f);

        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.doDialogue))]
        [HarmonyPrefix]
        public static bool OnDemonDialogueDoDialoguePostFix(object[] __args, DemonDialogue __instance)
        {
            if (_currentDialogueState == -1)
                _currentDialogueState = 0;
            else
                _currentDialogueState = (int)__args[0];
            __instance.dstate = 0;
            __instance.hideBtns();
            _txtBox.UpdateText("");
            __instance.dtxt(_dialogueStates[_currentDialogueState].dialogueText);
            __instance.btns(_dialogueStates[_currentDialogueState].option1Text, _dialogueStates[_currentDialogueState].option2Text, _dialogueStates[_currentDialogueState].option1DialogueID, _dialogueStates[_currentDialogueState].option2DialogueID);

            __instance.btn2obj.GetComponent<RectTransform>().anchoredPosition = _dialogueStates[_currentDialogueState].option2Text != "..." ? _btn2PositionCenter : _btn2PositionRight;

            Plugin.Instance.LogInfo("Event #" + _currentDialogueState);
            //Add dialogue specific events here
            switch (_currentDialogueState)
            {
                #region Chapter 1
                case 110001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 110002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    FlipSpriteAnimation(_soda, true);
                    break;
                case 110003:
                    FlipSpriteAnimation(_soda, true, 3f);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 110004:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 110005:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutralTalk, Color.white);
                    break;
                case 110006:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 110007:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    break;
                case 110100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.cheeredTrixie = true;
                    UpdateDialogueStates(1);
                    break;
                case 110200:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 110201:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _rightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 110202:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    break;
                case 110203:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    break;
                case 110206:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _leftCenterCharPosition, 0.9f, GetSecondDegreeAnimationFunction(), delegate
                    {
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    });
                    break;
                case 110204:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 110300:
                case 110400:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    DialogueFlags.isCompetitive = _currentDialogueState == 110300;
                    UpdateDialogueStates(1);
                    break;
                case 110402:
                    FlipSpriteAnimation(_trixiebell, true);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 110500:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    DialogueFlags.welcomedAppaloosa = true;
                    UpdateDialogueStates(1);
                    break;
                case 110600:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 110601:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 110602:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 110700:
                case 110800:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    DialogueFlags.presentedFriends = _currentDialogueState == 110700;
                    UpdateDialogueStates(1);
                    break;
                case 110701:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 110801:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    break;
                case 110802:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 110804:
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCenterCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 110805:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    break;
                case 110806:
                    FlipSpriteAnimation(_kaizyle, true, 1.1f);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleDispleased, Color.white);
                    break;
                case 110901:
                    FlipSpriteAnimation(_kaizyle, true, 0.5f);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 110902:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 110903:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    break;
                case 110904:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 110905:
                    FlipSpriteAnimation(_soda, true);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 2.65f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_trixiebell, true, .9f);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outLeftCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_beezerly, true, .8f);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_appaloosa, true, 1.2f);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outRightCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_kaizyle, true);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _outRightCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 210000, 2.65f));
                    break;

                case 110401:
                case 110803:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                #endregion

                #region Chapter 2

                #region Trixie
                case 210001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 210002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    break;
                case 210003:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 210004:
                    FlipSpriteLeftAnimation(_soda, true);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 210005:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 210006:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 210007:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 210008:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    break;
                case 210009:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 210010:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 210100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    break;
                case 210101:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment2, Color.white);
                    break;
                case 210102:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    break;
                case 210103:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSurprise, Color.white);
                    FlipSpriteLeftAnimation(_beezerly, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 210104:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    FlipSpriteRightAnimation(_trixiebell, false, 2f);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 0.8f, GetSecondDegreeAnimationFunction(), delegate
                    {
                        FlipSpriteRightAnimation(_beezerly, true);
                    });
                    break;
                case 210200:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaDeepSmug, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 210201:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    break;
                case 210202:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 210203:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 210204:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    FlipSpriteAnimation(_trixiebell, false);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(0.6f), delegate
                    {
                        FlipSpriteAnimation(_beezerly, false, 10f);
                        AnimationManager.AddNewTransformPositionAnimation(_beezerly, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    });
                    break;
                #endregion

                #region Beezerly
                case 220000:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    break;
                case 2200001:
                    FlipSpriteLeftAnimation(_beezerly, true);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    break;
                case 220001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction(0.6f));
                    break;
                case 220002:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);
                    break;
                case 220003:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    break;
                case 220004:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyThinking, Color.white);
                    break;
                case 220100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBleh, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    DialogueFlags.talkedShitAboutRock = true;
                    UpdateDialogueStates(2);
                    break;
                case 220101:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyChallenge, Color.white);
                    break;
                case 220200:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    break;
                case 220008:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    break;
                case 220009:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    break;
                case 220010:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    break;
                case 220011:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyThinking, Color.white);
                    break;
                case 220300:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, DialogueFlags.talkedShitAboutRock ? CharExpressions.BeezerlyAggro : CharExpressions.BeezerlyNeutral, Color.white);
                    FlipSpriteAnimation(_beezerly, false);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                #endregion

                #region Appaloosa
                case 54:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 55:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.didntPeekAppaloosaRoom && DialogueFlags.didntPeekKaizyleRoom ? CharExpressions.SodaNeutral : CharExpressions.SodaThinking, Color.white);
                    DialogueFlags.pickedAppaloosa = DialogueFlags.pickedKaizyle = false;
                    UpdateDialogueStates(2);
                    break;
                case 56:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _leftCenterCharPosition + new Vector3(.8f, 0, 0), 0.7f, GetSecondDegreeAnimationFunction(.8f), delegate
                      {
                          ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                          ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                          FlipSpriteAnimation(_beezerly, false, 5f);
                          AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction(), delegate
                          {
                              ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                          });
                      });
                    break;
                case 57:
                    _appaloosa.transform.position = _outLeftCharPosition; //tp her to the left xd YEET
                    DialogueFlags.pickedAppaloosa = DialogueFlags.didntPeekAppaloosaRoom = true;
                    UpdateDialogueStates(2);
                    break;
                case 58:
                    DialogueFlags.didntPeekAppaloosaRoom = false;
                    UpdateDialogueStates(2);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    FlipSpriteAnimation(_soda, true);
                    break;
                case 59:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 60:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 61:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    DialogueFlags.askedAppaloosaForHelp = true;
                    UpdateDialogueStates(2);
                    break;
                case 62:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white); //ThumbsUp / sure! emote
                    break;
                case 63:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 64:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 65:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 66:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 67:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    FlipSpriteLeftAnimation(_trixiebell, false);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                #endregion

                #region Kaizyle
                case 999999:
                    DialogueFlags.pickedKaizyle = DialogueFlags.didntPeekKaizyleRoom = true;
                    UpdateDialogueStates(2);
                    break;
                case 68:
                    DialogueFlags.didntPeekKaizyleRoom = false;
                    FlipSpriteAnimation(_kaizyle, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    DialogueFlags.botheredKaizyle = true;
                    UpdateDialogueStates(2);
                    break;
                case 69:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    break;
                case 70:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    DialogueFlags.askedKaizyleForHelp = true;
                    UpdateDialogueStates(2);
                    break;
                case 71:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 72:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition + new Vector3(1.2f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition + new Vector3(.4f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleConcern, Color.white);
                    break;
                case 74:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition + new Vector3(2.6f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition + new Vector3(.8f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.annoyedTheFuckOutOfKaizyle = true;
                    UpdateDialogueStates(2);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBeg, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    break;
                case 75:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition + new Vector3(4f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition + new Vector3(1.2f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 76:
                    FlipSpriteAnimation(_kaizyle, false, 2f);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(1.5f), delegate { ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white); });
                    break;
                case 77:
                    FlipSpriteAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.5f));
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleConcern, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaDeepSmug, Color.white);
                    break;
                case 78:
                    FlipSpriteAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.5f));
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 79:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    break;
                case 80:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizylePissed, Color.white);
                    break;
                case 81:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 82, 2.65f));
                    break;
                #endregion

                #endregion

                #region Chapter 3
                //START CHAPTER 3 
                #region TrixieDate
                case 82:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    FlipSpriteRightAnimation(_soda, false, 10f);
                    FlipSpriteAnimation(_trixiebell, false, 10f);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.TrixieBag, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _leftCenterCharPosition - new Vector3(1, 0), 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 83:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    FlipSpriteAnimation(_trixiebell, true);
                    break;
                case 84:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, DialogueFlags.cheeredTrixie ? CharExpressions.TrixiePleased : CharExpressions.TrixieSadge, Color.white);
                    break;
                case 85:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    break;
                case 86:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, DialogueFlags.cheeredTrixie ? CharExpressions.TrixieCompliment2 : CharExpressions.TrixieSadge, Color.white);
                    break;
                case 87:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, DialogueFlags.cheeredTrixie ? CharExpressions.TrixieAnxious : CharExpressions.TrixieSadge, Color.white);
                    break;
                case 88:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    break;
                case 89:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.mentionedTrixiePenguinPin = true;
                    UpdateDialogueStates(3);
                    break;
                case 90:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutralTalk, Color.white);
                    break;
                case 92:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.invitedTrixieOut = true;
                    UpdateDialogueStates(3);
                    break;
                case 93:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 94:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3, Color.white); //Trixie over excited
                    break;
                case 95:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    break;
                case 96:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAgree, Color.white); //TrixieThumbsUp!
                    break;
                case 97:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 98:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment2, Color.white); //TrixieWave
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 99, 2.65f));
                    break;
                case 99:
                    FlipSpriteLeftAnimation(_trixiebell, false, 10f);
                    FlipSpriteAnimation(_soda, false, 10f);
                    break;
                case 100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3, Color.white); //TrixieOverEXCITED
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    RecursiveTrixieAnimation();
                    break;
                case 101:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment2, Color.white);
                    break;
                case 102:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 103:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    DialogueFlags.sodaAteACookie = DialogueFlags.trixieAteACookie = true;
                    DialogueFlags.sharedCookieWithTrixie = true;
                    UpdateDialogueStates(3);
                    break;
                case 104:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 105:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 106:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSurprise, Color.white);
                    break;
                case 107:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 1080:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 108:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 109, 2.65f));
                    break;
                case 110:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    break;
                case 111:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 112:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.calledTrixieAFriend = true;
                    UpdateDialogueStates(3);
                    break;
                case 113:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment2, Color.white);
                    break;
                case 114:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 115:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3, Color.white);
                    break;
                case 117:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    DialogueFlags.mentionedTrixiePenguinPin = true;
                    UpdateDialogueStates(3);
                    break;
                case 1180:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 118:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 119:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 120:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 121:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 122:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    DialogueFlags.awkwardMomentWithTrixie = true;
                    UpdateDialogueStates(3);
                    break;
                case 123:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 124:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    FlipSpriteAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(0.6f));
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320001, 5.5f));
                    DialogueFlags.gtfoOfTheDateEarly = true;
                    UpdateDialogueStates(3);
                    break;
                case 125:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    DialogueFlags.toldTrixieAboutTheSmell = true;
                    UpdateDialogueStates(3);
                    break;
                case 126:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    break;
                case 127:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    break;
                case 128:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, DialogueFlags.awkwardMomentWithTrixie ? CharExpressions.TrixiePanic : CharExpressions.TrixieCompliment3, Color.white);
                    break;
                case 129:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 130:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    DialogueFlags.wannaMeetWithTrixieAgain = true;
                    UpdateDialogueStates(3);
                    break;
                case 131:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 132:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 133:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHoldCookie, Color.white); // SodaEatCookie
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, .2f, GetSecondDegreeAnimationFunction(0.001f), delegate
                    {
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaEat, Color.white);
                    });
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.sodaAteACookie = true;
                    UpdateDialogueStates(3);
                    break;
                case 134:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 135:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.sodaAteACookie = true;
                    DialogueFlags.threwCookieInGarbage = true;
                    UpdateDialogueStates(3);
                    break;
                case 136:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEat, Color.white); //SodaEatCookie
                    FlipSpriteAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1.5f, GetSecondDegreeAnimationFunction(0.6f), delegate
                    {
                        FlipSpriteAnimation(_soda, false, 10f);
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaMunch, Color.white);
                        AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.8f));
                    });
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white); //TrixieHungry
                    break;
                case 137:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHoldCookie, Color.white); //SodaHasCookie
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    DialogueFlags.sodaAteACookie = true;
                    UpdateDialogueStates(3);
                    break;
                case 138:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEat, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, .1f, GetSecondDegreeAnimationFunction(0.001f), delegate
                    {
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaMunch, Color.white);
                    });
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSurprise, Color.white);
                    break;
                case 139:
                    FlipSpriteAnimation(_soda, true);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.saidTheTruth = true;
                    UpdateDialogueStates(3);
                    break;
                case 140:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutralTalk, Color.white);
                    break;
                case 141:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 142:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 143:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBleh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    break;
                case 144:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 145:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 146:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    break;
                case 147:
                    FlipSpriteAnimation(_soda, true);
                    break;
                case 148:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    DialogueFlags.awkwardMomentWithTrixie = true;
                    UpdateDialogueStates(3);
                    break;
                case 149:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 150:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 151:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 152:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 153:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 154:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.8f));
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBeg, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition + new Vector3(.4f, 0, 0), 1.2f, GetSecondDegreeAnimationFunction(0.8f));
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    break;
                case 155:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 156, 2.65f));
                    break;
                case 156:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 3.2f, GetSecondDegreeAnimationFunction(0.15f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 3.2f, GetSecondDegreeAnimationFunction(0.15f));
                    DialogueFlags.walkedTrixieBackHome = true;
                    UpdateDialogueStates(3);
                    break;
                case 157:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 158:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 159:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 160:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutralTalk, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320001, 2.65f));
                    break;
                case 161:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaDeepSmug, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 162:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white); //Maybe less shoked?? 
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 163:
                    FlipSpriteAnimation(_soda, false);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outLeftCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 164, 2.65f));
                    DialogueFlags.walkedTrixieBackHome = true;
                    UpdateDialogueStates(3);
                    break;
                case 164:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 3.2f, GetSecondDegreeAnimationFunction(0.15f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 3.2f, GetSecondDegreeAnimationFunction(0.15f));
                    break;
                case 165:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 166:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    DialogueFlags.wantsToGoToAquarium = true;
                    UpdateDialogueStates(3);
                    break;
                case 167:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 168:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white); //SodaChuckle
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    break;
                case 169:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 170:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 171:
                    FlipSpriteAnimation(_soda, false);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 172, 2.65f));
                    break;
                case 172:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f));
                    break;
                case 173:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    break;
                case 174:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    break;
                case 175:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAgree, Color.white);
                    break;
                case 176:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment2, Color.white);
                    break;
                case 177:
                    Plugin.Instance.StartCoroutine(SpecialFadeOutScene(__instance, 178, 0f, 0.4f));
                    DialogueFlags.kissedTrixie = DialogueFlags.kissedSomeone = true;
                    UpdateDialogueStates(3);
                    break;
                case 178:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 179, 6f));
                    break;
                case 179:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieInLove, Color.white);
                    break;
                case 180:
                    FlipSpriteAnimation(_soda, false, .8f);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, DialogueFlags.kissedTrixie ? CharExpressions.TrixieCompliment3 : CharExpressions.TrixieCompliment2, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320001, 2.65f));
                    break;
                #endregion

                #region Beezerly Date
                //Chapter 3 part 2
                case 320001:
                    FlipSpriteAnimation(_beezerly, false, 10f);
                    FlipSpriteAnimation(_soda, false, 10f);
                    ChangeCharSprite(_beezerlySprite, DialogueFlags.talkedShitAboutRock ? CharExpressions.BeezerlyAggro : CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 320002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    break;
                case 320003:
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(1.2f));
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320004:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 330000, 2.65f)); //to Chap 3 part 3 transition
                    break;
                case 320005:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyThinking, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320006:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320007, 2.65f));
                    break;
                case 320007:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 320008:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320009:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 320010:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    DialogueFlags.askedIfFirstTime = true;
                    UpdateDialogueStates(3);
                    break;
                case 320011:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320012:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320100:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 320101:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320102:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    DialogueFlags.orderedBurger = true;
                    UpdateDialogueStates(3);
                    break;
                case 320013:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320200:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    DialogueFlags.listenedToTheBand = true;
                    UpdateDialogueStates(3);
                    break;
                case 320201:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320202:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    FlipSpriteRightAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction(.7f));
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction(.7f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320203, 2f));
                    break;
                case 320203:
                    FlipSpriteLeftAnimation(_appaloosa, false, 10f);
                    FlipSpriteRightAnimation(_soda, false, 10f);
                    FlipSpriteRightAnimation(_appaloosa, false, 10f);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.HornLordTalk, Color.white); //BAND DUDE LOL
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _centerCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 3202040:
                    __instance.csc.fadeMus(0, false);
                    Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("RockNBone.mp3", clip =>
                    {
                        __instance.csc.bgmus2.clip = clip;
                        __instance.csc.bgmus2.volume = 0f;
                        __instance.csc.bgmus2.Play();
                        __instance.csc.fadeMus(1, true);
                    }));
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.HornLordYeah, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 3202041:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.HornLordNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white); //BeezerlyOVEREXCITED
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    break;
                case 3202042:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);//Would be cool to have a dance animation for beez and soda
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320100, 2f));
                    break;
                case 3202050:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaCall, Color.white); //SODA SCREAMMMM
                    DialogueFlags.pickedBeezerlyFavoriteSong = true;
                    UpdateDialogueStates(3);
                    break;
                case 3202051:
                    __instance.csc.fadeMus(0, false);
                    Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("RockNBone.mp3", clip =>
                    {
                        __instance.csc.bgmus2.clip = clip;
                        __instance.csc.bgmus2.volume = 0f;
                        __instance.csc.bgmus2.Play();
                        __instance.csc.fadeMus(1, true);
                    }));
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.HornLordYeah, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    break;
                case 3202052:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.HornLordNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyHype, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    break;
                case 3202053:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHype, Color.white);
                    break;
                case 3202054:
                    FlipSpriteAnimation(_beezerly, true);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyPoint, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 3202055:
                    FlipSpriteAnimation(_beezerly, false);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outLeftCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.3f));
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _leftCharPosition, 1f, GetSecondDegreeAnimationFunction(), delegate { FlipSpriteRightAnimation(_beezerly, true); });
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition + new Vector3(1, 0, 0), 1.1f, GetSecondDegreeAnimationFunction(), delegate
                    {
                        danceSpriteIndex = danceIncrement = 1;
                        RecursiveSodaBeezerlyDanceAnimation();
                    });
                    DialogueFlags.dancedWithBeezerly = true;
                    UpdateDialogueStates(3);
                    break;
                case 3202057:
                    DialogueFlags.complimentedBeezerlyDancing = true;
                    UpdateDialogueStates(3);
                    break;
                case 3204057:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 3204058:
                    danceIncrement = 0;
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyInLove, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    break;
                case 3203055:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 3203056:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 3203057:
                    FlipSpriteLeftAnimation(_beezerly, false);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3203058, 3f)); // transition to same scene
                    break;
                case 3203058:
                    danceIncrement = 0;
                    FlipSpriteRightAnimation(_beezerly, false);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 3203059:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3204060, 2.65f)); // transition to outside
                    break;
                case 3204059:
                    DialogueFlags.kissedBeezerly = DialogueFlags.kissedSomeone = true;
                    UpdateDialogueStates(3);
                    Plugin.Instance.StartCoroutine(SpecialFadeOutScene(__instance, 3204159, 0f, 0.4f)); // transition to kissing scene
                    break;
                case 3204159:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3204060, 6f));
                    break;
                case 3205059:
                    danceIncrement = 0;
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3204060, 2.65f)); // transition to outside
                    break;
                case 3204060:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    FlipSpriteAnimation(_soda, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 2.65f, GetSecondDegreeAnimationFunction(0.2f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _centerCharPosition, 2.65f, GetSecondDegreeAnimationFunction(0.2f));
                    break;
                case 3204061:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 3204062:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 3204063:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 3204064:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    break;
                case 3204065:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    FlipSpriteRightAnimation(_soda, false, 0.8f);
                    FlipSpriteRightAnimation(_beezerly, false, 0.8f);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 330000, 2.65f)); // transition to part 3 :D
                    break;
                case 320400:
                    ChangeCharSprite(_beezerlySprite, DialogueFlags.orderedBurger ? CharExpressions.BeezerlyNeutral : CharExpressions.BeezerlyAggro, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    DialogueFlags.askedAboutTheFood = true;
                    UpdateDialogueStates(3);
                    break;
                case 320401:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320500:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 320014:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    break;
                case 320015:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320016:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyPassion, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    break;
                case 320700:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyWhat, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    break;
                case 320701:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyUh, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 320800:
                case 320801:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    DialogueFlags.convincedBeezerly = true;
                    UpdateDialogueStates(3);
                    break;
                case 320802:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 320017:
                case 320600:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    DialogueFlags.agreedWithBeezerly = true;
                    UpdateDialogueStates(3);
                    break;
                case 320018:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 320019:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    break;
                case 320020:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320021:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 320022:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 321000:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyUh, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBleh, Color.white);
                    DisplayAchievement("Discuss Burger", "Yes. This burger is made out of burger.");
                    break;
                case 321001:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyChallenge, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 321002:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 321003:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 321004:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyWhat, Color.white); 
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    break;
                case 321005:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyChallenge, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaFightMe, Color.white);
                    break;
                case 321006:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyChallenge, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHype, Color.white);
                    break;
                case 321007:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyChallenge, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHype, Color.white);
                    break;
                case 3210011:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyUh, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaCall, Color.white);
                    break;
                case 3210012:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyPassion, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 320023:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    DialogueFlags.likedTheBurger = true;
                    UpdateDialogueStates(3);
                    break;
                case 320024:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    break;
                case 320025:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaMunch, Color.white);
                    break;
                case 320026:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 320027:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 320028:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3204065, 2.65f)); // transition to outside
                    break;
                case 320030:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 330000, 2.65f)); //To Chap 3 part 3 transition
                    break;
                #endregion

                #region Old Appaloosa Date
                /*case 330000:
                    FlipSpriteLeftAnimation(_appaloosa, false, 10f);
                    FlipSpriteLeftAnimation(_soda, false, 10f);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 330001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white); // AppaloosaAgree
                    break;
                case 3300011:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 330002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 330003:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white); // SodaTrombone (?)
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaTrombone (?)
                    break;
                case 330004:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaWow
                    break;
                case 330005:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 330006:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 330007:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white); // AppaloosaPointToSelf
                    break;
                case 3310001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaDisappointed
                    DialogueFlags.disinterestedAppaloosa = true;
                    UpdateDialogueStates(3);
                    break;
                case 331000:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHype, Color.white);
                    ChangeCharSprite(_appaloosaSprite, (DialogueFlags.disinterestedAppaloosa ? CharExpressions.AppaloosaNeutral : CharExpressions.AppaloosaLOL), Color.white); // AppaloosaDisappointed
                    break;
                case 33100020:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 331000201:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331000202:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 3310002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    FlipSpriteRightAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 331001, 2.65f)); //To the jazz bar!
                    break;
                case 33100021:
                case 33101021:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    DialogueFlags.obsessAppaloosa = _currentDialogueState == 33100021;
                    UpdateDialogueStates(3);
                    break;
                case 331001: //Jazz bar
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 331002:
                    FlipSpriteLeftAnimation(_soda, true);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white); // AppaloosaPointToSelf
                    break;
                case 331200:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLeanAway, Color.white);
                    break;
                case 331100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 331101:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 331102:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331103:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaPointToSelf
                    break;
                case 3311031:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaBlush
                    break;
                case 3311032:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 3311033:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331104:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, (DialogueFlags.unimpressedAppaloosa ? CharExpressions.AppaloosaNeutral : CharExpressions.AppaloosaNeutral), Color.white); // AppaloosaDisappointed (unimpressed)
                    break;
                case 331105:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 331106:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaPointToSelf
                    break;
                case 331120:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaDisappointed
                    break;
                case 33110:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaBlush
                    break;
                case 331111:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 331112:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331113:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.unimpressedAppaloosa ? CharExpressions.SodaWheezeRW : CharExpressions.SodaHype, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.unimpressedAppaloosa ? CharExpressions.AppaloosaLeanAway : CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331114:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.unimpressedAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaHype, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.unimpressedAppaloosa ? CharExpressions.AppaloosaNeutral : CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaDisappointed
                    break;
                case 3311141:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.flirtAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.flirtAppaloosa ? CharExpressions.AppaloosaNeutral : CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 3311142:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.flirtAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.flirtAppaloosa ? CharExpressions.AppaloosaLeanAway : CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaBlush
                    // KISS KISS KISS KISS
                    break;
                case 3311150:
                    DialogueFlags.kissedAppaloosa = DialogueFlags.kissedSomeone = true;
                    UpdateDialogueStates(3);
                    Plugin.Instance.StartCoroutine(SpecialFadeOutScene(__instance, 3311151, 0f, 0.4f)); //KISS
                    break;
                case 3311151:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 331115, 6.5f));
                    break; 
                case 331115:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.kissedAppaloosa ? CharExpressions.SodaStressLight : CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.kissedAppaloosa ? CharExpressions.AppaloosaLeanAway : CharExpressions.AppaloosaNeutral, Color.white); // AppaloosaBlush
                    break;
                case 331116:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 340000, 2.65f)); //To Chap 3 part 4 transition
                    break;*/
                #endregion

                #region New Appaloosa Date
                case 330000:
                    FlipSpriteLeftAnimation(_appaloosa, false, 10f);
                    FlipSpriteLeftAnimation(_soda, false, 10f);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 330001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaAgree, Color.white); 
                    break;
                case 3300011:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaAgree, Color.white);
                    break;
                case 330002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 330003:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBone, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaBone, Color.white);
                    break;
                case 330004:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaWow, Color.white);
                    break;
                case 330005:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 330006:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 330007:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 3310001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaDisappointed, Color.white); 
                    DialogueFlags.disinterestedAppaloosa = true;
                    break;
                case 331000:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaHype, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 33100020:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaAgree, Color.white);
                    break;
                case 331000201: // STRANGE SCENE TRANSITION
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    FlipSpriteRightAnimation(_appaloosa, false, 1f);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 331000202:
                    break;
                case 33100021:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    DialogueFlags.obsessAppaloosa = true;
                    break;
                case 33101021:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 33100023:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white); // STRANGE SCENE TRANSITION
                    break;
                case 3310002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(0.8f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 331001, 2.65f)); //To the jazz bar!
                    break;
                case 331001:
                    FlipSpriteLeftAnimation(_appaloosa, false, 10f);
                    FlipSpriteRightAnimation(_soda, false, 10f);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 3310092:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _centerCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 3310093:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    break;
                case 3310094:
                    break;
                case 331002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    FlipSpriteLeftAnimation(_soda, false, 1f);
                    break;
                case 331200:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white); 
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaDisappointed, Color.white);
                    break;
                case 331100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331101:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 331102:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331103:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white); // AppaloosaPointToSelf
                    break;
                case 3311031:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.obsessAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 3311032:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.obsessAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.obsessAppaloosa ?  CharExpressions.AppaloosaBlush : CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 3311033:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 331104:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331220:
                    ChangeCharSprite(_sodaSprite, (DialogueFlags.unimpressedAppaloosa ? (DialogueFlags.obsessAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaNeutral) : CharExpressions.SodaNeutral), Color.white);
                    ChangeCharSprite(_appaloosaSprite, (DialogueFlags.unimpressedAppaloosa ? (DialogueFlags.obsessAppaloosa ? CharExpressions.AppaloosaLeanAway : CharExpressions.AppaloosaNeutralTalk) : CharExpressions.AppaloosaNeutralTalk), Color.white);
                    break;
                case 331105:
                    ChangeCharSprite(_sodaSprite, (DialogueFlags.unimpressedAppaloosa ? (DialogueFlags.obsessAppaloosa ? CharExpressions.SodaWow : CharExpressions.SodaThinking) : CharExpressions.SodaNeutralTalk), Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 331106:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 331120:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.obsessAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.obsessAppaloosa ? CharExpressions.AppaloosaNeutral : CharExpressions.AppaloosaNeutralTalk, Color.white);
                    break;
                case 33110:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaBlush, Color.white);
                    break;
                case 331111:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaBlush, Color.white);
                    break;
                case 331112:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaBlush, Color.white);
                    break;
                case 331113:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 3311131:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.isCompetitive ? CharExpressions.SodaHype : CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.isCompetitive ? CharExpressions.AppaloosaLeanAway : CharExpressions.AppaloosaBlush, Color.white);
                    break;
                case 331114:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.unimpressedAppaloosa ? CharExpressions.SodaEmbarrassedLight : CharExpressions.SodaHype, Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.unimpressedAppaloosa ? CharExpressions.AppaloosaLeanAway : CharExpressions.AppaloosaBlush, Color.white);
                    break;
                case 3311141:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaBlush, Color.white);
                    break;
                case 3311142:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 331142:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLeanAway, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.awkwardAppaloosa = true;
                    break;
                case 3311150:
                    DialogueFlags.kissedAppaloosa = DialogueFlags.kissedSomeone = true;
                    UpdateDialogueStates(3);
                    Plugin.Instance.StartCoroutine(SpecialFadeOutScene(__instance, 3311151, 0f, 0.4f)); //KISS
                    break;
                case 3311151:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 331115, 6.5f));
                    break;
                case 331115:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.awkwardAppaloosa ? CharExpressions.SodaWheezeRW : (DialogueFlags.kissedAppaloosa ? CharExpressions.SodaInLove : CharExpressions.SodaHype), Color.white);
                    ChangeCharSprite(_appaloosaSprite, DialogueFlags.awkwardAppaloosa ? CharExpressions.AppaloosaLeanAway : (DialogueFlags.kissedAppaloosa ? CharExpressions.AppaloosaNeutral : CharExpressions.AppaloosaLOL), Color.white);
                    break;
                case 331116:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 340000, 2.65f)); //To Chap 3 part 4 transition
                    break;
                #endregion

                #region Kaizyle Date
                case 340000:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 3f, GetSecondDegreeAnimationFunction(0.3f), delegate
                    {
                        FlipSpriteRightAnimation(_appaloosa, false);
                        AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outRightCharPosition, 2.25f, GetSecondDegreeAnimationFunction(0.3f));
                    });
                    //Whole fkn animation scene to make trixie look silly
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 20f, GetSecondDegreeAnimationFunction(1f), delegate
                    {
                        ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                        FlipSpriteLeftAnimation(_trixiebell, false, 10f);
                        AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 2.5f, GetSecondDegreeAnimationFunction(1f), delegate
                        {
                            if (DialogueFlags.kissedTrixie)
                            {
                                ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieInLove);
                                AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 2f, GetSecondDegreeAnimationFunction(0.0001f), delegate
                                {
                                    FlipSpriteRightAnimation(_trixiebell, false, 1.5f);
                                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3);
                                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 2f, GetSecondDegreeAnimationFunction(1f));
                                });
                            }
                            else
                            {
                                ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge);
                                FlipSpriteRightAnimation(_trixiebell, false, 1.5f);
                                AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 2f, GetSecondDegreeAnimationFunction(1f));
                            }
                        });
                    });

                    break;
                case 340001:
                    FlipSpriteLeftAnimation(_kaizyle, true);
                    FlipSpriteLeftAnimation(_soda, true);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 340002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 340003:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400031:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    break;
                case 3400032:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400033:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizylePissed, Color.white);
                    break;
                case 3400034:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    _appaloosa.transform.position = _outRightCharPosition;
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outRightCharPosition, 6.66f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 3400035 }, __instance);
                    });
                    break;
                case 3400035:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    break;
                case 340004:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleDispleased, Color.white);
                    break;
                case 340100:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    break;
                case 340101:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    break;
                case 340102: //GTFO ENDING
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    FlipSpriteRightAnimation(_kaizyle, false);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 340005:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    DialogueFlags.wannaKnowAboutKaizyle = true;
                    UpdateDialogueStates(3);
                    break;
                case 340006:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    break;
                case 340007:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    break;
                case 340008:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    break;
                case 340009:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFine, Color.white);
                    break;
                case 340010:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 0.3f, GetSecondDegreeAnimationFunction(0.001f), delegate { ChangeCharSprite(_sodaSprite, CharExpressions.SodaHype, Color.white); });
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340011:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 340012:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340013:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleDispleased, Color.white);
                    break;
                case 34010810:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaCall, Color.white); //YIPPIES
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizylePissed, Color.white);
                    DialogueFlags.saidYippies = true;
                    UpdateDialogueStates(3);
                    break;
                case 3401081:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3400090, 2.65f)); //To Downtown
                    break;
                case 34000810:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 34000811:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 3400081:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 3400090, 2.65f)); //To Downtown
                    break;

                case 3400091:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 1f, GetSecondDegreeAnimationFunction(), delegate
                    {
                        FlipSpriteLeftAnimation(_kaizyle, false);
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaDeepSmug, Color.white);
                    });
                    break;
                case 34000101:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 34000111:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400011:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleConcern, Color.white);
                    break;
                case 3400012:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleDispleased, Color.white);
                    break;
                case 3400013:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400014:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleShrug, Color.white);
                    break;
                case 340200:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    DialogueFlags.choosedGlissandogs = true;
                    UpdateDialogueStates(3);
                    break;
                case 340201:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaDeepSmug, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    break;
                case 340202:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    break;
                case 340203:
                    FlipSpriteRightAnimation(_kaizyle, true);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    break;
                case 340204:
                    FlipSpriteLeftAnimation(_kaizyle, true);
                    break;
                case 340305:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBleh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleConcern, Color.white);
                    DialogueFlags.arguedAboutGlissandogs = true;
                    UpdateDialogueStates(3);
                    break;
                case 340306:
                case 340308:
                case 340310:
                case 340312:
                case 340314:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBleh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFightMe, Color.white);
                    if (_currentDialogueState == 340314)
                        AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _centerCharPosition, 1f, GetSecondDegreeAnimationFunction(1.2f));
                    break;
                case 340307:
                case 340309:
                case 340311:
                case 340313:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaFightMe, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    if (_currentDialogueState == 340313)
                        AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition - new Vector3(-1, 0), 1.1f, GetSecondDegreeAnimationFunction(0.3f));
                    break;
                case 340315:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    break;
                case 340316:
                    FlipSpriteRightAnimation(_kaizyle, false, 1.2f);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(1.2f));
                    break;
                case 340317:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBreaking, Color.white);
                    break;
                case 340318:
                    FlipSpriteLeftAnimation(_soda, false, 0.3f);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 2.5f, GetSecondDegreeAnimationFunction(0.2f));
                    break;
                case 340206:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    break;
                case 340407:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleConcern, Color.white);
                    DialogueFlags.overReactedAboutKaizyleHotdogs = true;
                    UpdateDialogueStates(3);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 340408 }, __instance);
                    });
                    break;
                case 340408:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 340409 }, __instance);
                    });
                    break;
                case 340409:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 340410 }, __instance);
                    });
                    break;
                case 340410:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 3404101 }, __instance);
                    });
                    break;
                case 3404101:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizylePissed, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 340411 }, __instance);
                    });
                    break;
                case 340411:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                    {
                        OnDemonDialogueDoDialoguePostFix(new object[] { 340412 }, __instance);
                    });
                    break;
                case 340412:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition + new Vector3(1, 0, 0), 0.5f, GetSecondDegreeAnimationFunction(.0001f), delegate
                      {
                          OnDemonDialogueDoDialoguePostFix(new object[] { 340413 }, __instance);
                      });
                    break;
                case 340413:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaPlead, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    break;
                case 340207:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340208:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340015:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340016:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 340017, 2.65f)); //To Ice Cream yeehaw yum yum
                    break;
                case 340300:
                case 340400:
                    FlipSpriteLeftAnimation(_soda, false);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    DialogueFlags.gotIceCream = _currentDialogueState == 340300;
                    DialogueFlags.gotSundae = _currentDialogueState == 340400;
                    UpdateDialogueStates(3);
                    break;
                case 3400171:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleCat, Color.white);
                    break;
                case 340018:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340020:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaMunch, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFine, Color.white);
                    break;
                case 3400201:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    break;
                case 340021:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340022:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 3400221:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340023:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleEnamored, Color.white);
                    DialogueFlags.complimentedKaizyle = true;
                    UpdateDialogueStates(3);
                    break;
                case 340024:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFlatteredLookUp, Color.white);
                    break;
                case 340025:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaMunch, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400261:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleWTF, Color.white);
                    FlipSpriteRightAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1.5f, GetSecondDegreeAnimationFunction(0.9f), delegate
                    {
                        FlipSpriteLeftAnimation(_soda, false, 10f);
                        ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                        AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    });
                    DialogueFlags.threwIceCreamAway = true;
                    UpdateDialogueStates(3);
                    break;
                case 3400262:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400263:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3400264:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.gotIceCream ? CharExpressions.SodaNeutral : CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleShrug, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 340026, 2.65f)); //To Chap 4 transition
                    break;
                case 340026:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction(0.9f));
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCenterCharPosition, 1f, GetSecondDegreeAnimationFunction(0.9f), delegate
                    {
                        FlipSpriteLeftAnimation(_kaizyle, true);
                    });
                    break;
                case 340027:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 340028:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340029:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleConcern, Color.white);
                    break;
                case 340030:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340031:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 340131:
                    FlipSpriteRightAnimation(_kaizyle, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCenterCharPosition - new Vector3(1,0,0), 1f, GetSecondDegreeAnimationFunction(0.9f), delegate
                    {
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);//SodaReleaf after sitting
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.9f), delegate
                    {
                        FlipSpriteLeftAnimation(_kaizyle, true);
                    });
                    break;
                case 340132:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaMunch, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340133:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleCat, Color.white);
                    break;
                case 340134:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleCat, Color.white);
                    break;
                case 340135:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNom, Color.white);
                    break;
                case 340136:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 3401361:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaInLove, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFlattered, Color.white);
                    break;
                case 340137:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFlatteredLookUp, Color.white);
                    break;
                case 340138:
                    Plugin.Instance.StartCoroutine(SpecialFadeOutScene(__instance, 340139, 0f, 0.4f)); //KISSING SCENE BOI
                    DialogueFlags.kissedKaizyle = DialogueFlags.kissedSomeone = true;
                    UpdateDialogueStates(3);
                    break;
                case 340139:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 340239, 6.5f));
                    break;
                case 340238:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340239:
                    ChangeCharSprite(_sodaSprite, DialogueFlags.kissedKaizyle ? CharExpressions.SodaInLove : CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, DialogueFlags.kissedKaizyle ? CharExpressions.KaizyleLove : CharExpressions.KaizyleFine, Color.white);
                    break;
                case 340240:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleLove, Color.white);
                    break;
                case 340241:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFlattered, Color.white);
                    break;
                case 340032:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340033:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, DialogueFlags.kissedKaizyle ? CharExpressions.KaizyleFlattered : CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 340034:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340035:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleFine, Color.white);
                    break;
                case 340036:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleShrug, Color.white);
                    break;
                case 340037:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 340038:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 340039:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 350000:
                    FlipSpriteRightAnimation(_soda, false);
                    FlipSpriteRightAnimation(_kaizyle, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction(0.9f));
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(0.9f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410000, 2.65f)); //To Chap 4 transition
                    break;
                #endregion

                #endregion

                #region Chapter 4
                case 410000:
                    //Flip the fack out of everyone
                    FlipSpriteLeftAnimation(_soda, false, 10f);
                    FlipSpriteLeftAnimation(_trixiebell, false, 10f);
                    FlipSpriteLeftAnimation(_beezerly, false, 10f);
                    FlipSpriteLeftAnimation(_appaloosa, false, 10f);
                    FlipSpriteLeftAnimation(_kaizyle, false, 10f);
                    break;
                case 410001:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    break;
                case 410002:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.trixiePresent = true;
                    UpdateDialogueStates(4);
                    break;
                case 410003:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyBump, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _centerCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.beezerlyPresent = true;
                    UpdateDialogueStates(4);
                    break;
                case 410004:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutralTalk, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.appaloosaPresent = true;
                    UpdateDialogueStates(4);
                    break;
                case 410005:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.kaizylePresent = true;
                    UpdateDialogueStates(4);
                    break;
                case 410006:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 410007:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3, Color.white);
                    break;
                case 410008:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyPassion, Color.white);
                    break;
                case 410009:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 410010:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    break;
                case 410011:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePanic, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLeanAway, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleUm, Color.white);
                    break;
                case 4100111:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 410501:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 410502:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieInLove, Color.white);
                    break;
                case 410503:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyInLove, Color.white);
                    break;
                case 410504:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaLOL, Color.white);
                    break;
                case 410505:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    break;

                //perform with Trixie
                case 410100:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410101, 2.65f));
                    DialogueFlags.performedWithTrixie = true;
                    UpdateDialogueStates(4);
                    break;

                //perform with Beezerly
                case 410200:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410201, 2.65f));
                    DialogueFlags.performedWithBeezerly = true;
                    UpdateDialogueStates(4);
                    break;

                //perform with Appaloosa
                case 410300:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410301, 2.65f));
                    DialogueFlags.performedWithAppaloosa = true;
                    UpdateDialogueStates(4);
                    break;

                //perform with Kaizyle
                case 410400:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410401, 2.65f));
                    DialogueFlags.performedWithKaizyle = true;
                    UpdateDialogueStates(4);
                    break;

                case 410012:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410013, 2.65f));
                    DialogueFlags.performedSolo = true;
                    UpdateDialogueStates(4);
                    break;
                case 410506:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410507, 2.65f));
                    DialogueFlags.performedGroup = true;
                    UpdateDialogueStates(4);
                    break;
                    #endregion
            }

            return false;
        }


        public static IEnumerator SpecialFadeOutScene(DemonDialogue __instance, int nextDialogueID, float delay, float speedMultiplier = 1f)
        {
            yield return new WaitForSeconds(delay);

            __instance.csc.fadeoutpanel.GetComponent<Image>().color = new Color(.75f, .75f, .75f);
            __instance.csc.fadeoutpanel.transform.localScale = new Vector3(2f, 0.001f, 1f);
            __instance.csc.fadeoutpanel.SetActive(true);

            AnimationManager.AddNewTransformScaleAnimation(__instance.csc.fadeoutpanel, new Vector3(2f, 2f, 1f), 1.3f / speedMultiplier, GetSecondDegreeAnimationFunction(speedMultiplier), delegate
            {
                switch (nextDialogueID)
                {
                    case 178:
                        _soda.transform.position = _outLeftCharPosition;
                        _trixiebell.transform.position = _outLeftCharPosition;
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("SpecialMomentTrixie.png");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(1, 1, 1);
                        break;
                    case 3204159:
                        _soda.transform.position = _outLeftCharPosition;
                        _beezerly.transform.position = _outLeftCharPosition;
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("SpecialMomentBeezerly.png");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(1, 1, 1);
                        break;
                    case 3311151:
                        _soda.transform.position = _outLeftCharPosition;
                        _appaloosa.transform.position = _outLeftCharPosition;
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("SpecialMomentAppaloosa.png");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(1, 1, 1);
                        break;
                    case 340139:
                        _soda.transform.position = _outLeftCharPosition;
                        _kaizyle.transform.position = _outLeftCharPosition;
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("SpecialMomentKaizyle.png");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(1, 1, 1);
                        break;
                }
                SpecialFadeInScene(__instance, nextDialogueID, speedMultiplier);
            });
        }

        public static IEnumerator FadeOutScene(DemonDialogue __instance, int nextDialogueID, float delay = 0)
        {

            yield return new WaitForSeconds(delay);

            __instance.csc.fadeoutpanel.transform.localScale = new Vector3(2f, 0.001f, 1f);
            __instance.csc.fadeoutpanel.SetActive(true);
            __instance.csc.fadeMus(0, false);
            __instance.csc.fadeMus(1, false);
            __instance.csc.fadeoutpanel.GetComponent<Image>().color = Color.black;
            AnimationManager.AddNewTransformScaleAnimation(__instance.csc.fadeoutpanel, new Vector3(2f, 2f, 1f), 2f, GetSecondDegreeAnimationFunction(), delegate
            {
                switch (nextDialogueID)
                {
                    //end chapter 1
                    case 210000:
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("ClassroomEvening.png");
                        _txtBox.UpdateText("");
                        _soda.transform.position = _leftCenterCharPosition;
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral);
                        _trixiebell.transform.position = _outRightCharPosition;
                        _soda.transform.position = _leftCenterCharPosition;
                        __instance.csc.fadeMus(1, true);
                        UpdateDialogueStates(2);
                        LogChapter1States();
                        LogScores();
                        break;

                    //end chapter 2
                    case 82:
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        _txtBox.UpdateText("");
                        __instance.csc.bgmus1.time = 0;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.bgmus1.Play();
                        ResetCharacterPositions();
                        _soda.transform.position = _outRightCharPosition;
                        _trixiebell.transform.position = _outLeftCharPosition;
                        _beezerly.transform.position = _outLeftCharPosition;
                        UpdateDialogueStates(3);
                        LogChapter2States();
                        LogScores();
                        break;

                    //Penguin Caffee Scene
                    case 99:
                        ResetCharacterPositions();
                        __instance.csc.fadeMus(0, true);
                        _soda.transform.position = _outLeftCharPosition;
                        _trixiebell.transform.position = _outLeftCharPosition;
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("PenguinCafe.png");
                        _txtBox.UpdateText("");
                        break;

                    //Breathing practice transition
                    case 109:
                    //TimeSkip
                    case 150:
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                        ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                        __instance.csc.fadeMus(0, true);
                        _txtBox.UpdateText("");
                        break;
                    //Street Night Scene
                    case 156:
                    case 164:
                        FlipSpriteLeftAnimation(_soda, false, 10f);
                        FlipSpriteRightAnimation(_trixiebell, false, 10f); 
                        _soda.transform.position = _outLeftCharPosition + new Vector3(1, 0, 0);
                        _trixiebell.transform.position = _outLeftCharPosition;
                        _appaloosa.transform.position = _outLeftCharPosition;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("StreetNight.png");
                        _txtBox.UpdateText("");
                        break;
                    case 172:
                        FlipSpriteLeftAnimation(_soda, false, 10f);
                        FlipSpriteRightAnimation(_trixiebell, false, 10f);
                        _soda.transform.position = _outLeftCharPosition + new Vector3(1, 0, 0);
                        _trixiebell.transform.position = _outLeftCharPosition;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("TrixieHouseNight.png");
                        _txtBox.UpdateText("");
                        break;
                    //Trixie Back from kissing
                    case 179:
                        __instance.csc.fadeMus(0, true);
                        _soda.transform.position = _centerCharPosition - new Vector3(1, 0, 0);
                        _trixiebell.transform.position = _leftCenterCharPosition;
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                        ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3, Color.white);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("TrixieHouseNight.png");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(.6f, .6f, .6f);
                        _txtBox.UpdateText("");
                        break;
                    //Beezerly's date
                    case 320001:
                        ResetCharacterPositions();
                        _beezerly.transform.position = _outRightCharPosition;
                        FlipSpriteRightAnimation(_soda, true, 10f);
                        _txtBox.UpdateText("");
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        LogChapter3Part1States();
                        LogScores();
                        break;

                    //Hard Rock Cafe
                    case 320007:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        _soda.transform.position = _outLeftCharPosition;
                        _beezerly.transform.position = _outRightCharPosition;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("HardRockCafe.png");
                        break;

                    //Hard Rock Cafe Stage
                    case 320203:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        _appaloosa.transform.position = _outLeftCharPosition;
                        _soda.transform.position = _outRightCharPosition + new Vector3(1, 0, 0);
                        _beezerly.transform.position = _outRightCharPosition;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("HardRockCafeTableAndStage.png");
                        break;

                    //Timetravel forward
                    case 3203058:
                    case 320100:
                        _txtBox.UpdateText("");
                        __instance.csc.fadeMus(0, true);
                        break;

                    //Rock Cafe Outside
                    case 3204060:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        _soda.transform.position = _outLeftCharPosition + new Vector3(1, 0, 0);
                        _beezerly.transform.position = _outLeftCharPosition;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("HardRockCafeOutside.png");
                        break;

                    //Appaloosa's Date
                    case 330000:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("Chapter3p3Music.mp3", clip =>
                        {
                            __instance.csc.bgmus2.clip = clip;
                            __instance.csc.bgmus2.volume = 0f;
                            __instance.csc.bgmus2.Play();
                        }));
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        LogChapter3Part2States();
                        LogScores();
                        break;
                    case 331001:
                        ResetCharacterPositions();
                        FlipSpriteRightAnimation(_soda, false, 10f);
                        _soda.transform.position = _outRightCharPosition;
                        _txtBox.UpdateText("");
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("JazzClub.png");
                        break;

                    case 331115:
                        FlipSpriteRightAnimation(_soda, false, 10f);
                        _soda.transform.position = _leftCenterCharPosition;
                        _appaloosa.transform.position = _rightCharPosition;
                        _txtBox.UpdateText("");
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("JazzClub.png");
                        break;
                    //Kaizyle Date
                    case 340000:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        FlipSpriteRightAnimation(_soda, false);
                        _soda.transform.position = _leftCharPosition;
                        FlipSpriteRightAnimation(_trixiebell, false, 10f);
                        _trixiebell.transform.position = _leftCenterCharPosition;
                        FlipSpriteRightAnimation(_beezerly, false, 10f);
                        _beezerly.transform.position = _centerCharPosition;
                        FlipSpriteLeftAnimation(_appaloosa, false, 10f);
                        _appaloosa.transform.position = _rightCenterCharPosition;
                        FlipSpriteRightAnimation(_kaizyle, false, 10f);
                        _kaizyle.transform.position = _rightCharPosition;
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        LogChapter3Part3States();
                        LogScores();
                        break;
                    //Ice Cream
                    case 340017:
                        _soda.transform.position = _leftCenterCharPosition;
                        _kaizyle.transform.position = _rightCenterCharPosition;
                        FlipSpriteRightAnimation(_soda, false, 10f);
                        FlipSpriteLeftAnimation(_kaizyle, false, 10f);
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                        ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                        _txtBox.UpdateText("");
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("IceCreamCafe.png");
                        break;
                    //DownTown
                    case 3400090:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        _kaizyle.transform.position = _rightCenterCharPosition + new Vector3(1, 0, 0);
                        FlipSpriteRightAnimation(_kaizyle, false, 10f);
                        FlipSpriteLeftAnimation(_soda, false, 10f);
                        ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("Downtown.png");
                        break;
                    //Park
                    case 340026:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        FlipSpriteRightAnimation(_kaizyle, false, 10f);
                        _kaizyle.transform.position = _outLeftCharPosition;
                        _soda.transform.position = _outLeftCharPosition - new Vector3(1.2f, 0, 0);
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("ParkBench.png");
                        break;

                    case 340239:
                        _txtBox.UpdateText("");
                        FlipSpriteRightAnimation(_kaizyle, false, 10f);
                        FlipSpriteLeftAnimation(_soda, false, 10f);
                        _kaizyle.transform.position = _rightCharPosition;
                        _soda.transform.position = _rightCenterCharPosition - new Vector3(1, 0, 0);
                        __instance.csc.fadeMus(1, true);
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("ParkBench.png");
                        break;
                    #region Chapter 4 transitions
                    //end Chapter 3 part 4
                    case 410000:
                        ResetCharacterPositions();
                        UpdateDialogueStates(4);
                        _trixiebell.transform.position = _outRightCharPosition;
                        _txtBox.UpdateText("");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("Backstage.png");
                        __instance.csc.fadeMus(1, true);
                        LogChapter3Part4States();
                        LogScores();
                        break;

                    //perform with Trixie
                    case 410101:
                        SingleTrackData loveHasNoEndTrack = TrackLookup.toTrackData(TrackLookup.lookup(_loveHasNoEndTrackref));
                        if (!GlobalVariables.alltrackslist_custom.Contains(loveHasNoEndTrack))
                            GlobalVariables.alltrackslist_custom.Add(loveHasNoEndTrack);
                        GlobalVariables.chosen_track = _loveHasNoEndTrackref;
                        GlobalVariables.chosen_track_data = loveHasNoEndTrack;
                        LogChapter4States();
                        LogScores();
                        SceneManager.LoadScene("loader");
                        return;

                    //perform with Beezerly
                    case 410201:
                        SingleTrackData letBeYourselfTrack = TrackLookup.toTrackData(TrackLookup.lookup(_letBeYourselfTrackref));
                        if (!GlobalVariables.alltrackslist_custom.Contains(letBeYourselfTrack))
                            GlobalVariables.alltrackslist_custom.Add(letBeYourselfTrack);
                        GlobalVariables.chosen_track = _letBeYourselfTrackref;
                        GlobalVariables.chosen_track_data = letBeYourselfTrack;
                        LogChapter4States();
                        LogScores();
                        SceneManager.LoadScene("loader");
                        return;

                    //perform with Appaloosa
                    case 410301:
                        SingleTrackData lateNightJazTrack = TrackLookup.toTrackData(TrackLookup.lookup(_lateNightJazTrackref));
                        if (!GlobalVariables.alltrackslist_custom.Contains(lateNightJazTrack))
                            GlobalVariables.alltrackslist_custom.Add(lateNightJazTrack);
                        GlobalVariables.chosen_track = _lateNightJazTrackref;
                        GlobalVariables.chosen_track_data = lateNightJazTrack;
                        LogChapter4States();
                        LogScores();
                        SceneManager.LoadScene("loader");
                        return;

                    //perform with Kaizyle
                    case 410401:
                        SingleTrackData pathOfDiscoveries = TrackLookup.toTrackData(TrackLookup.lookup(_pathOfDiscoveriesTrackref));
                        if (!GlobalVariables.alltrackslist_custom.Contains(pathOfDiscoveries))
                            GlobalVariables.alltrackslist_custom.Add(pathOfDiscoveries);
                        GlobalVariables.chosen_track = _pathOfDiscoveriesTrackref;
                        GlobalVariables.chosen_track_data = pathOfDiscoveries;
                        LogChapter4States();
                        LogScores();
                        SceneManager.LoadScene("loader");
                        return;
                    //Solo performance KEKW
                    case 410013:
                        SingleTrackData memoriesOfYou = TrackLookup.toTrackData(TrackLookup.lookup(_memoriesOfYouTrackref));
                        if (!GlobalVariables.alltrackslist_custom.Contains(memoriesOfYou))
                            GlobalVariables.alltrackslist_custom.Add(memoriesOfYou);
                        GlobalVariables.chosen_track = _memoriesOfYouTrackref;
                        GlobalVariables.chosen_track_data = memoriesOfYou;
                        LogChapter4States();
                        LogScores();
                        SceneManager.LoadScene("loader");
                        return;
                    //HAREM FOR JOE WOOOO
                    case 410506:
                        SingleTrackData loveFlipTrack = TrackLookup.toTrackData(TrackLookup.lookup(_loveFlipTrackref));
                        if (!GlobalVariables.alltrackslist_custom.Contains(loveFlipTrack))
                            GlobalVariables.alltrackslist_custom.Add(loveFlipTrack);
                        GlobalVariables.chosen_track = _loveFlipTrackref;
                        GlobalVariables.chosen_track_data = loveFlipTrack;
                        LogChapter4States();
                        LogScores();
                        SceneManager.LoadScene("loader");
                        return;
                        #endregion
                }
                FadeInScene(__instance, nextDialogueID);
            });
        }

        public static void LogChapter1States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER1 STATES:");
            Plugin.Instance.LogInfo("   cheeredTrixie: " + DialogueFlags.cheeredTrixie);
            Plugin.Instance.LogInfo("   isCompetitive: " + DialogueFlags.isCompetitive);
            Plugin.Instance.LogInfo("   welcomedAppaloosa: " + DialogueFlags.welcomedAppaloosa);
            Plugin.Instance.LogInfo("   presentedFriends: " + DialogueFlags.presentedFriends);
            Plugin.Instance.LogInfo("   calmedKaizyleDown: " + DialogueFlags.calmedKaizyleDown);
            Plugin.Instance.LogInfo("-----------------------------");
        }
        public static void LogChapter2States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER2 STATES:");
            Plugin.Instance.LogInfo("   offeredPracticeWithTrixie: " + DialogueFlags.offeredPracticeWithTrixie);
            Plugin.Instance.LogInfo("   talkedShitAboutRock: " + DialogueFlags.talkedShitAboutRock);
            Plugin.Instance.LogInfo("   offeredIdeaToBeezerly: " + DialogueFlags.offeredIdeaToBeezerly);
            Plugin.Instance.LogInfo("   pickedAppaloosa: " + DialogueFlags.pickedAppaloosa);
            Plugin.Instance.LogInfo("   pickedKaizyle: " + DialogueFlags.pickedKaizyle);
            Plugin.Instance.LogInfo("   askedAppaloosaForHelp: " + DialogueFlags.askedAppaloosaForHelp);
            Plugin.Instance.LogInfo("   offeredIdeaToBeezerly: " + DialogueFlags.offeredIdeaToBeezerly);
            Plugin.Instance.LogInfo("   askedKaizyleForHelp: " + DialogueFlags.askedKaizyleForHelp);
            Plugin.Instance.LogInfo("   annoyedTheFuckOutOfKaizyle: " + DialogueFlags.annoyedTheFuckOutOfKaizyle);
            Plugin.Instance.LogInfo("-----------------------------");
        }

        public static void LogChapter3Part1States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER 3 PART 1 STATES:");
            Plugin.Instance.LogInfo("   mentionedTrixiePenguinPin: " + DialogueFlags.mentionedTrixiePenguinPin);
            Plugin.Instance.LogInfo("   invitedTrixieOut: " + DialogueFlags.invitedTrixieOut);
            Plugin.Instance.LogInfo("   sharedCookieWithTrixie: " + DialogueFlags.sharedCookieWithTrixie);
            Plugin.Instance.LogInfo("   saidTheTruth: " + DialogueFlags.saidTheTruth);
            Plugin.Instance.LogInfo("   calledTrixieAFriend: " + DialogueFlags.calledTrixieAFriend);
            Plugin.Instance.LogInfo("   awkwardMomentWithTrixie: " + DialogueFlags.awkwardMomentWithTrixie);
            Plugin.Instance.LogInfo("   toldTrixieAboutTheSmell: " + DialogueFlags.toldTrixieAboutTheSmell);
            Plugin.Instance.LogInfo("   gtfoOfTheDateEarly: " + DialogueFlags.gtfoOfTheDateEarly);
            Plugin.Instance.LogInfo("   wannaMeetWithTrixieAgain: " + DialogueFlags.wannaMeetWithTrixieAgain);
            Plugin.Instance.LogInfo("   walkedTrixieBackHome: " + DialogueFlags.walkedTrixieBackHome);
            Plugin.Instance.LogInfo("   sodaAteACookie: " + DialogueFlags.sodaAteACookie);
            Plugin.Instance.LogInfo("   trixieAteACookie: " + DialogueFlags.trixieAteACookie);
            Plugin.Instance.LogInfo("   threwCookieInGarbage: " + DialogueFlags.threwCookieInGarbage);
            Plugin.Instance.LogInfo("   kissedTrixie: " + DialogueFlags.kissedTrixie);
            Plugin.Instance.LogInfo("   wantsToGoToAquarium: " + DialogueFlags.wantsToGoToAquarium);
            Plugin.Instance.LogInfo("-----------------------------");
        }
        public static void LogChapter3Part2States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER 3 PART 2 STATES:");
            Plugin.Instance.LogInfo("   orderedBurger: " + DialogueFlags.askedIfFirstTime);
            Plugin.Instance.LogInfo("   agreedWithBeezerly: " + DialogueFlags.orderedBurger);
            Plugin.Instance.LogInfo("   agreedWithBeezerly: " + DialogueFlags.agreedWithBeezerly);
            Plugin.Instance.LogInfo("   convincedBeezerly: " + DialogueFlags.convincedBeezerly);
            Plugin.Instance.LogInfo("   askedAboutTheFood: " + DialogueFlags.askedAboutTheFood);
            Plugin.Instance.LogInfo("   listenedToTheBand: " + DialogueFlags.listenedToTheBand);
            Plugin.Instance.LogInfo("   pickedBeezerlyFavoriteSong: " + DialogueFlags.pickedBeezerlyFavoriteSong);
            Plugin.Instance.LogInfo("   dancedWithBeezerly: " + DialogueFlags.dancedWithBeezerly);
            Plugin.Instance.LogInfo("   kissedBeezerly: " + DialogueFlags.kissedBeezerly);
            Plugin.Instance.LogInfo("   complimentedBeezerlyDancing: " + DialogueFlags.complimentedBeezerlyDancing);
            Plugin.Instance.LogInfo("-----------------------------");
        }

        public static void LogChapter3Part3States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER 3 PART 3 STATES:");
            Plugin.Instance.LogInfo("   unimpressedAppaloosa: " + DialogueFlags.unimpressedAppaloosa);
            Plugin.Instance.LogInfo("   kissedAppaloosa: " + DialogueFlags.kissedAppaloosa);
            Plugin.Instance.LogInfo("   awkwardAppaloosa: " + DialogueFlags.awkwardAppaloosa);
            Plugin.Instance.LogInfo("   flirtAppaloosa: " + DialogueFlags.flirtAppaloosa);
            Plugin.Instance.LogInfo("   obsessAppaloosa: " + DialogueFlags.obsessAppaloosa);
        }

        public static void LogChapter3Part4States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER 3 PART 4 STATES:");
            Plugin.Instance.LogInfo("   wannaKnowAboutKaizyle: " + DialogueFlags.wannaKnowAboutKaizyle);
            Plugin.Instance.LogInfo("   saidYippies: " + DialogueFlags.saidYippies);
            Plugin.Instance.LogInfo("   choosedGlissandogs: " + DialogueFlags.choosedGlissandogs);
            Plugin.Instance.LogInfo("   arguedAboutGlissandogs: " + DialogueFlags.arguedAboutGlissandogs);
            Plugin.Instance.LogInfo("   overReactedAboutKaizyleHotdogs: " + DialogueFlags.overReactedAboutKaizyleHotdogs);
            Plugin.Instance.LogInfo("   complimentedKaizyle: " + DialogueFlags.complimentedKaizyle);
            Plugin.Instance.LogInfo("   threwIceCreamAway: " + DialogueFlags.threwIceCreamAway);
            Plugin.Instance.LogInfo("   gotIceCream: " + DialogueFlags.gotIceCream);
            Plugin.Instance.LogInfo("   gotSundae: " + DialogueFlags.gotSundae);
            Plugin.Instance.LogInfo("   kissedKaizyle: " + DialogueFlags.kissedKaizyle);
            Plugin.Instance.LogInfo("-----------------------------");
        }
        public static void LogChapter4States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER 4 STATES:");
            Plugin.Instance.LogInfo("   trixiePresent: " + DialogueFlags.trixiePresent);
            Plugin.Instance.LogInfo("   beezerlyPresent: " + DialogueFlags.beezerlyPresent);
            Plugin.Instance.LogInfo("   appaloosaPresent: " + DialogueFlags.appaloosaPresent);
            Plugin.Instance.LogInfo("   kaizylePresent: " + DialogueFlags.kaizylePresent);
            Plugin.Instance.LogInfo("   performedWithTrixie: " + DialogueFlags.performedWithTrixie);
            Plugin.Instance.LogInfo("   performedWithBeezerly: " + DialogueFlags.performedWithBeezerly);
            Plugin.Instance.LogInfo("   performedWithAppaloosa: " + DialogueFlags.performedWithAppaloosa);
            Plugin.Instance.LogInfo("   performedWithKaizyle: " + DialogueFlags.performedWithKaizyle);
            Plugin.Instance.LogInfo("   performedSolo: " + DialogueFlags.performedSolo);
            Plugin.Instance.LogInfo("   performedGroup: " + DialogueFlags.performedGroup);
            Plugin.Instance.LogInfo("-----------------------------");
        }

        public static void LogScores()
        {
            Plugin.Instance.LogInfo("CURRENT SCORES:");
            Plugin.Instance.LogInfo("   trixieScore: " + _scoreData.trixieScore);
            Plugin.Instance.LogInfo("   appaloosaScore: " + _scoreData.appaloosaScore);
            Plugin.Instance.LogInfo("   beezerlyScore: " + _scoreData.beezerlyScore);
            Plugin.Instance.LogInfo("   kaizyleScore: " + _scoreData.kaizyleScore);
            Plugin.Instance.LogInfo("-----------------------------");
        }

        public static void FadeInScene(DemonDialogue __instance, int nextDialogueID)
        {
            __instance.csc.fadeoutpanel.transform.localScale = new Vector3(2f, 2f, 1f);
            AnimationManager.AddNewTransformScaleAnimation(__instance.csc.fadeoutpanel, new Vector3(2f, 0.001f, 1f), 1.5f, GetSecondDegreeAnimationFunction(), delegate
            {
                __instance.csc.fadeoutpanel.SetActive(false);
                OnDemonDialogueDoDialoguePostFix(new object[] { nextDialogueID }, __instance);
            });

        }

        public static void SpecialFadeInScene(DemonDialogue __instance, int nextDialogueID, float speedMultiplier = 1f)
        {
            __instance.csc.fadeoutpanel.transform.localScale = new Vector3(2f, 2f, 1f);
            AnimationManager.AddNewTransformScaleAnimation(__instance.csc.fadeoutpanel, new Vector3(2f, 0.001f, 1f), 1.2f / speedMultiplier, GetSecondDegreeAnimationFunction(speedMultiplier), delegate
            {
                __instance.csc.fadeoutpanel.SetActive(false);
                OnDemonDialogueDoDialoguePostFix(new object[] { nextDialogueID }, __instance);
            });

        }
        public static Dictionary<int, DialogueData> _dialogueStates = new Dictionary<int, DialogueData>();


        // ID STRUCTURE [CHAPTER#][PART#][PATH][DIALOGUE]
        //                  X        X     XX      XX
        // EX: Chap 1, Part 2, Path 32, Dialogue 7 == 123207
        public static Dictionary<int, DialogueData> GetDialogueChapter1And2() => new Dictionary<int, DialogueData>
        {
            #region CHAPTER 1 INTRO
            {0,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 1: raise of the conductor's baton",
                    option2DialogueID = 110000
                }
            },
            {110000,
                new DialogueData()
                {
                    dialogueText = $"???: I can't wait for the music competition this year.",
                    option2DialogueID = 110001
                }
            },
            {110001,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I've been practicing day and night, but that doesn't bother me.",
                    option2DialogueID = 110006
                }
            },
            {110006,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Tooting is the only thing I've ever loved.",
                    option2DialogueID = 110007
                }
            },
            {110007,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: But... is that really all I w-",
                    option2DialogueID = 110002
                }
            },
            {110002,
                new DialogueData()
                {
                    dialogueText = $"The door creaks open.",
                    option2DialogueID = 110003,

                }
            },
            {110003,
                new DialogueData()
                {
                    dialogueText = $"???: Oh!... Hi, {_sodaColoredName}. Have you seen my sheet music?",
                    option2DialogueID = 110004
                }
            },
            {110004,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey, {_trixieColoredName}. I think I saw a binder on the piano.",
                    option2DialogueID = 110005
                }
            },
            {110005,
                new DialogueData()
                {
                    dialogueText =  $"{_trixieColoredName}: Thanks. I've been so nervous about this year's competition; I nearly left my trombone at home!",
                    option1Text = "Cheer",
                    option1DialogueID = 110100,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2f
                    },
                    option2Text = "Ignore",
                    option2DialogueID = 110205,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -2f
                    }

                }
            },
            {110100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Don't worry! We're tromboners through and through. I'm sure you'll do great.",
                    option2DialogueID = 110200
                }
            },
            {110205,
                new DialogueData()
                {
                    dialogueText = $"Trixie has always been one to worry over everything, but I can't say I blame her this week.",
                    option2DialogueID = 110200
                }
            },
            {110200,
                new DialogueData()
                {
                    dialogueText = $"The door squeals as it flies open, revealing a confident grin and a large trombone.",
                    option2DialogueID = 110201
                }
            },
            {110201,
                new DialogueData()
                {
                    dialogueText = $"???: 'Sup, music nerds. What's going on?",
                    option2DialogueID = 110202
                }
            },
            {110202,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey, {_beezerlyColoredName}. Just prepping before the competition.",
                    option2DialogueID = 110203
                }
            },
            {110203,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Yeah-yeah, the competition. Whatever. I'm just here to jam and have some fun.",
                    option2DialogueID = 110206
                }
            },
            {110206,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName} walks up behind me.",
                    option2DialogueID = 110204
                }
            },
            {110204,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName} [Whispering]: I don't think she takes music very seriously.",
                    option1Text = "I'm competitive",
                    option1DialogueID = 110300,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                        beezerlyScore = -1,
                    },
                    option2Text = "I'm casual",
                    option2DialogueID = 110400,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -1,
                        beezerlyScore = 2,
                    }
                }
            },
            {110300,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, everyone has their own way of enjoying music. Maybe we can show her the fun in competition too.",
                    option2DialogueID = 110401
                }
            },
            {110400,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey, everyone has their own way of enjoying music. That's what makes it special.",
                    option2DialogueID = 110401
                }
            },
            {110401,
                new DialogueData()
                {
                    dialogueText = $"The door cries out in pain as a sleek, professional-looking trombone swings into view.",
                    option2DialogueID = 110402
                }
            },
            {110402,
                new DialogueData()
                {
                    dialogueText = $"???: So. Is this where the cool tromboners hang out?",
                    option1Text = "That's us!",
                    option1DialogueID = 110500,
                    option1Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                        trixieScore = -1,
                    },
                    option2Text = "Cool...?",
                    option2DialogueID = 110600,
                }
            },
            {110500,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Darn tootin'! Are you gonna join us?",
                    option2DialogueID = 110602
                }
            },
            {110600,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I guess so... What brings you here?",
                    option2DialogueID = 110601
                }
            },
            {110602,
                new DialogueData()
                {
                    dialogueText = $"The girl laughs.",
                    option2DialogueID = 110601
                }
            },
            {110601,
                new DialogueData()
                {
                    dialogueText = $"???: Well, I heard there were some talented players in here, so I thought I'd scope out the competition. The name's {_appaloosaColoredName}.",
                    option1Text = "Introduce friends",
                    option1DialogueID = 110700,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 1,
                        beezerlyScore = 1,
                    },
                    option2Text = "Introduce myself",
                    option2DialogueID = 110800,
                    option2Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                    }
                }
            },
            {110700,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Nice to meet you, {_appaloosaColoredName}. This is {_trixieColoredName} and {_beezerlyColoredName}, and I'm {_sodaColoredName}!",
                    option2DialogueID = 110701
                }
            },
            {110800,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Nice to meet you, competition. The name's {_sodaColoredName}.",
                    option2DialogueID = 110801
                }
            },
            {110701,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Cool names. You guys wanna jam?",
                    option2DialogueID = 110802
                }
            },
            {110801,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: {_sodaColoredName}, huh? I like your style. Wanna jam?",
                    option2DialogueID = 110802
                }
            },
            {110802,
                new DialogueData()
                {
                    dialogueText = $"Trixiebell opens her mouth to speak, but all we hear is the baboon-like shriek of the doorhinge.",
                    option2DialogueID = 110803
                }
            },
            {110803,
                new DialogueData()
                {
                    dialogueText = $"A silver trombone enters the room, followed swiftly by a brown-haired tromboner.",
                    option2DialogueID = 110804
                }
            },
            {110804,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Good afternoon. I'm {_kaizyleColoredName}, and I'm here to rehearse for the competition.",
                    option2DialogueID = 110805
                }
            },
            {110805,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Awesome, another classical snob.",
                    option2DialogueID = 110806
                }
            },
            {110806,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I beg your pardon? I just so happen to come from a long line of respected tromboners, and I take my craft very seriously.",
                    option2DialogueID = 110900
                }
            },
            {110900,
                new DialogueData()
                {
                    dialogueText = $"Beezerly is as aggressive as ever, but this newcomer isn't backing down either. This could get ugly fast.",
                    option2DialogueID = 110901
                }
            },
            {110901,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Whoa there, Beezerly. Nice to meet you, Kaizyle.",
                    option2DialogueID = 110902
                }
            },
            {110902,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Let's turn it down a notch. We're all here for the same reason, right? To make beautiful, succulent music?",
                    option2DialogueID = 110903
                }
            },
            {110903,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName} purses her lips.",
                    option2DialogueID = 110904
                }
            },
            {110904,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yes, I suppose you're right. Let's get started, shall we?",
                    option2DialogueID = 110905
                }
            },
            {110905,
                new DialogueData()
                {
                    dialogueText = $"[END OF CHAPTER 1]",
                    option2Text = "",
                    option2DialogueID = 0
                }
            },
            #endregion

            #region CHAPTER 2 
            {210000,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 2: Tromboner Talk",
                    option2DialogueID = 210001
                }
            },
            {210001,
                new DialogueData()
                {
                    dialogueText = $"Satisfied with my day, I set about packing my trombone back in its case.",
                    option2DialogueID = 210002
                }
            },
            {210002,
                new DialogueData()
                {
                    dialogueText = $"I wonder how everyone else is doing.",
                    option2DialogueID = 210003
                }
            },

            #region Trixie Interaction
            {210003,
                new DialogueData()
                {
                    dialogueText = $"Back in the music room, I pull up a chair as the door sings the song of its people for the umpteenth time.",
                    option2DialogueID = 210004
                }
            },
            {210004,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Whew, that was some good tromboning!",
                    option2DialogueID = 210005
                }
            },
            {210005,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey! Are you done for the day, too?",
                    option2DialogueID = 210006
                }
            },
            {210006,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Yep. Any more practice and my arm is gonna fall off.",
                    option2DialogueID = 210007
                }
            },
            {210007,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Haha, I feel the same way. Still, it's hard not to be excited. The competition is just a week away!",
                    option2DialogueID = 210008
                }
            },
            {210008,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ...",
                    option2DialogueID = 210009
                }
            },
            {210009,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ... how are you feeling about it?",
                    option2DialogueID = 210010
                }
            },
            {210010,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Oh, I dunno... I've been practicing so much, but I'm still terrified that I'll beef it on stage.",
                    option1Text = "Suggest break",
                    option1DialogueID = 210100,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                    option2Text = "Ignore hint",
                    option2DialogueID = 210200,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -3,
                    },
                }
            },
            {210100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I know how you feel. Maybe we should take a break sometime soon.",
                    option2DialogueID = 210101
                }
            },
            {210101,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: That sounds lovely.",
                    option2DialogueID = 210102
                }
            },
            {210102,
                new DialogueData()
                {
                    dialogueText = $"Is she... blushing? Wait, did I just ask her ou-",
                    option2DialogueID = 210103
                }
            },
            {210103,
                new DialogueData()
                {
                    dialogueText = $"The door hollers in protest as {_beezerlyColoredName} strolls in",
                    option2DialogueID = 210104
                }
            },
            {210104,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName} shoots up out of her chair, grabs her case, and rushes out of the room, slamming the door as it howls in pain.",
                    option2DialogueID = 220000
                }
            },
            {210200,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Everything will be fine; no need to freak out!",
                    option2DialogueID = 210201
                }
            },
            {210201,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Um, thanks?",
                    option2DialogueID = 210202
                }
            },
            {210202,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Sure!",
                    option2DialogueID = 210203
                }
            },
            {210203,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ...I should get back to practicing.",
                    option2DialogueID = 210204
                }
            },
            {210204,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName} gets up quickly and leaves, almost slamming into Beezerly as the door howls its rusty displeasure.",
                    option2DialogueID = 220000
                }
            },
            #endregion

            #region Beezerly Interaction
            {220000,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: What's her deal?",
                    option2DialogueID = 2200001
                }
            },
            {2200001,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I don't know... was it something I said?",
                    option2DialogueID = 220001
                }
            },
            {220001,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName} swings a chair in front of her and plops down into it.",
                    option2DialogueID = 220002
                }
            },
            {220002,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Anyway. Ready for your big performance?",
                    option2DialogueID = 220003
                }
            },
            {220003,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: More or less. What are you gonna play?",
                    option2DialogueID = 220004
                }
            },
            {220004,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Eh, haven't decided yet. Maybe something rock'n'roll.",
                    option1Text = "Ew, rock?",
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = -11,
                    },
                    option1DialogueID = 220100,
                    option2Text = "Haven't decided???",
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 2
                    },
                    option2DialogueID = 220200
                }
            },
            {220100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Wait, seriously? I get that it succeeded jazz, and even led to funk, but when you combine those two, you get... junk.",
                    option2DialogueID = 220101
                }
            },
            {220101,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: ... go to hell, Soda.",
                    option2DialogueID = 220300
                }
            },
            {220200,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Seriously? What have you been doing all this time?",
                    option2DialogueID = 220008
                }
            },
            {220008,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I told you. I'm just here to have a good time. I might play something punk, or I might pull an étude out of my butt.",
                    option2DialogueID = 220009
                }
            },
            {220009,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I don't like being boxed in.",
                    option2DialogueID = 220010
                }
            },
            {220010,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That's pretty impressive. I wish I could come up with something like that on the fly.",
                    option2DialogueID = 220011
                }
            },
            {220011,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Maybe we can come up with something together! I gotta skedaddle. See you around, Soda!",
                    option2DialogueID = 220300
                }
            },
            {220300,
                new DialogueData()
                {
                    dialogueText = $"The door rattles as she slams it behind her, leaving me alone with its caterwauling hinges.",
                    option2DialogueID = 54
                }
            },
            {54,
                new DialogueData()
                {
                    dialogueText = $"I leave the room. The hinges' dismay is quickly drowned out by two wildly different pieces of music.",
                    option2DialogueID = 55,
                }
            },
            {55,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.didntPeekKaizyleRoom && DialogueFlags.didntPeekAppaloosaRoom ? $"Nah, I don't wanna bother anyone... I should just head home." : $"A jazzy melody in my left ear, and a solemn classical piece in my right.",
                    option1Text = DialogueFlags.didntPeekAppaloosaRoom ? "" : "Go left",
                    option1DialogueID = 57,
                    option2Text = DialogueFlags.didntPeekKaizyleRoom ? DialogueFlags.didntPeekAppaloosaRoom ? "..." : "" : "Go right",
                    option2DialogueID = DialogueFlags.didntPeekKaizyleRoom && DialogueFlags.didntPeekAppaloosaRoom ? 81 : 999999,
                }
            },
            #endregion

            #region Appaloosa Interaction
            {57,
                new DialogueData()
                {
                    dialogueText = $"I hear a jaunty tune coming from inside. Dare I peek in?",
                    option1Text = "Yes",
                    option1DialogueID = 58,
                    option1Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                    },
                    option2Text = "No",
                    option2DialogueID = 55,
                }
            },
            {58,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName} sits by a closed piano, jubilantly tromboning her heart out. I open the door and poke my head inside. The music stops.",
                    option2DialogueID = 59,
                }
            },
            {59,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: {_appaloosaColoredName}, your improvisation skills are incredible. How do you do it?",
                    option2DialogueID = 60,
                }
            },
            {60,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Oh, hey there! It's all about feeling the music, you know? You gotta let go of your inhibitions and just let the music flow through you.",
                    option1Text = "Teach me!",
                    option1DialogueID = 61,
                    option1Score = new ScoreData()
                    {
                        appaloosaScore = 2,
                    },
                    option2Text = "Let her practice",
                    option2DialogueID = 64,
                    option2Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                    },
                }
            },
            {61,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That sounds amazing. Can you show me how to improvise like you do?",
                    option2DialogueID = 62,
                }
            },
            {62,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Sure thing, {_sodaColoredName}. Let's jam!",
                    option2DialogueID = 63,
                }
            },
            {63,
                new DialogueData()
                {
                    dialogueText = $"The next hour is spent in a wild trombone duet. The two of us have a blast, and I almost don't want to leave.",
                    option2DialogueID = 66,
                }
            },
            {64,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, I'll leave you to it. Good luck!",
                    option2DialogueID = 65
                }
            },
            {65,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Thanks, {_sodaColoredName}. I'll see you around, okay?",
                    option2DialogueID = 67,
                }
            },
            {66,
                new DialogueData()
                {
                    dialogueText = $"Sadly, homework beckons for the both of us.",
                    option2DialogueID = 81,
                }
            },
            {67,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: See you around.",
                    option2DialogueID = 81,
                }
            },
            #endregion

            #region Kaizyle Interaction
            {999999,
                new DialogueData()
                {
                    dialogueText = $"An impeccable rendition of a classical tune plays behind the door. Dare I peek in?",
                    option1Text = "Yes",
                    option1DialogueID = 68,
                    option1Score = new ScoreData()
                    {
                        kaizyleScore = -1,
                    },
                    option2Text = "No",
                    option2DialogueID = 55,
                    option2Score = new ScoreData()
                    {
                        kaizyleScore = 3,
                    }
                }
            },
            {68,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName} sits by a closed piano, tooting estudiously. I open the door and poke my head inside. The music stops.",
                    option2DialogueID = 69,
                }
            },
            {69,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I take my training very seriously. What do you want, {_sodaColoredName}?",
                    option1Text = "Teach me!",
                    option1DialogueID = 70,
                    option2Text = "Let her practice",
                    option2DialogueID = 77,
                    option2Score = new ScoreData()
                    {
                        kaizyleScore = 1,
                    }
                }
            },
            {70,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That's really cool. Do you have any tips for me on how to improve my technique?",
                    option2DialogueID = 71,
                }
            },
            {71,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I really need to finish my practice. We can talk later.",
                    option1Text = "Beg for help",
                    option1DialogueID = 72,
                    option1Score = new ScoreData()
                    {
                        kaizyleScore = -2,
                    },
                    option2Text = "Let her practice",
                    option2DialogueID = 77,
                    option2Score = new ScoreData()
                    {
                        kaizyleScore = 1,
                    },
                }
            },
            {72,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: PLEASE, {_kaizyleColoredName}! I really need to improve my technique and I'd love to become as good as you in the future.",
                    option2DialogueID = 73,
                }
            },
            {73,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I told you. Not now. I need to practice.",
                    option1Text = "Keep begging",
                    option1DialogueID = 74,
                    option1Score = new ScoreData()
                    {
                        kaizyleScore = -2,
                    },
                    option2Text = "Let her practice",
                    option2DialogueID = 78,
                    option2Score = new ScoreData()
                    {
                        kaizyleScore = 1,
                    },
                }
            },
            {74,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE",
                    option2DialogueID = 75,
                }
            },
            {75,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE",
                    option2DialogueID = 76,
                }
            },
            {76,
                new DialogueData()
                {
                    dialogueText = $"Without a word, {_kaizyleColoredName} packs up and leaves.",
                    option2DialogueID = 81,
                }
            },
            {77,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Fine, I'll leave you to it. I'll see you later, though!",
                    option2DialogueID = 79,
                }
            },
            {78,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Pardon me. I'll be going now. Bye!",
                    option2DialogueID = 80,
                }
            },
            {79,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ... that was annoying.",
                    option2DialogueID = 81,
                }
            },
            {80,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ...you little brat.",
                    option2DialogueID = 81,
                }
            },
            {81,
                new DialogueData()
                {
                    dialogueText = $"[End of Chapter 2]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            #endregion

            #endregion
        };


        public static Dictionary<int, DialogueData> GetDialogueChapter3() => new Dictionary<int, DialogueData>()
        {
            #region TrixieDate
            {82,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 3: Chamber Music",
                    option2DialogueID = 83,
                }
            },
            {83,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_trixieColoredName}, what's new?",
                    option2DialogueID = 84,
                }
            },
            {84,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.cheeredTrixie ?
                    $"{_trixieColoredName}: Oh, hi {_sodaColoredName}. I'm great! Just practicing." :
                    $"{_trixieColoredName}: Oh, hi {_sodaColoredName}. I'm doing okay, I guess. Practicing... y'know?",
                    option2DialogueID = DialogueFlags.cheeredTrixie ? 85 : 87,
                }
            },
            {85,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thanks for cheering me up earlier. It really helped me out and I appreciate it.",
                    option2DialogueID = 86,
                }
            },
            {86,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: {(DialogueFlags.cheeredTrixie ? "You really are very talented." : "")}",
                    option2DialogueID = 87,
                }
            },
            {87,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: {(DialogueFlags.cheeredTrixie ? "Thank you. But..." : "")}I'm just so nervous about performing in front of an audience that size.",
                    option2DialogueID = 88,
                }
            },
            {88,
                new DialogueData()
                {
                    dialogueText = $"I look around the room for a quick change in topic before the silence gets too awkward.",
                    option1Text = "Penguin pin",
                    option1DialogueID = DialogueFlags.cheeredTrixie ? 89 : 117,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 1f
                    },
                    option2Text = "Window",
                    option2DialogueID = 123,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -5f
                    }
                }
            },
            {89,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That’s an neat pin on your bag. Isn't it kinda random though?",
                    option2DialogueID = 90,
                }
            },
            {90,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Not at all! It's a pin from the aquarium. Ever since I was kid, I’ve loved how soft and cuddly penguins look.",
                    option2DialogueID = 91,
                }
            },
            {91,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: I begged my parents to let me buy it from the gift shop! They had no intention of leaving without it, though. At least not once I started crying.",
                    option1Text = "Invite her out",
                    option1DialogueID = 92,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                    option2Text = "Compliment her",
                    option2DialogueID = 125,
                    option2Score = new ScoreData()
                    {
                        trixieScore = 5,
                    },
                }
            },
            {92,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh... actually, I known how we can take a break from practice now!",
                    option2DialogueID = 93,
                }
            },
            {93,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Have you seen the penguin cafe that opened in town? They have all sorts of marine-themed stuff. It's adorable.",
                    option2DialogueID = 94,
                }
            },
            {94,
                new DialogueData()
                {
                    dialogueText = $"Trixie's eyes light up.",
                    option2DialogueID = 95,
                }
            },
            {95,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Yeah, and it's not too far from here. We could go after school today if you want.",
                    option2DialogueID = 96,
                }
            },
            {96,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Absolutely! That would be so awesome!",
                    option2DialogueID = 97,
                }
            },
            {97,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Great! I'll meet you outside after class. Don't worry about the competition for now. Just focus on having a good time with the penguins.",
                    option2DialogueID = 98,
                }
            },
            {98, // Transition to cafe
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thank you so much, {_sodaColoredName}. You're the best.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {99,
                new DialogueData()
                {
                    dialogueText = $"This penguin cafe was the right choice.",
                    option2DialogueID = 100,
                }
            },
            {100,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Wow, look at all the penguins! They're so adorable!",
                    option2DialogueID = 101,
                }
            },
            {101,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Yeah, they're pretty cute! And check out these penguin cookies!",
                    option1Text = "Share",
                    option1DialogueID = 102,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                    option2Text = "Don't share",
                    option2DialogueID = 133,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                }
            },
            {102,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Want to split one with me?",
                    option2DialogueID = _scoreData.trixieScore >= 5 ? 103 : 134,
                }
            },
            {103,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Yesss! They look delicious.",
                    option2DialogueID = 104,
                }
            },
            {104,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You know, {_trixieColoredName}, you don't have to be nervous about the competition.",
                    option2DialogueID = 105,
                }
            },
            {105,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I used to have the same problem when I first started performing in front of people.",
                    option2DialogueID = 106,
                }
            },
            {106,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Really? What did you do?",
                    option1Text = "Truth",
                    option1DialogueID = 139,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                    option2Text = "Lie",
                    option2DialogueID = 107,
                    option2Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                }
            },
            {107,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I, uhhhh, I... learned some breathing techniques! Would you like me to show you?",
                    option2DialogueID = 1080,
                }
            },
            {1080,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Sure! I think that will help me calm down and focus as well.",
                    option2DialogueID = 108,
                }
            },
            {108, //Breathing practice transition
                new DialogueData()
                {
                    dialogueText = $"Surrounded by penguins, we breathe deeply and regularly. Huh. That actually works really well.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {109,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Just remember,take deep breaths and visualize yourself succeeding.",
                    option2DialogueID = 110,
                }
            },
            {110,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You're already a phenomenal tromboner. Just be yourself and have fun up there.",
                    option2DialogueID = 111,
                }
            },
            {111,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thanks, {_sodaColoredName}. You always know just what to say to make me feel better.",
                    option1Text = "Friends",
                    option1DialogueID = 112,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -10,
                    },
                    option2Text = "Penguin Joke",
                    option2DialogueID = 113,
                    option2Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                }
            },
            {112,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That's what friends are for.",
                    option2DialogueID = 114,
                }
            },
            {113,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Who knows, maybe one day you'll even get to perform for the penguins themselves!",
                    option2DialogueID = 115,
                }
            },
            {114,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: I'm glad we're friends, {_sodaColoredName}", //FRIEND ZONNNNEDDDDDDDDDDDDDDDDDDD EXDEE
                    option2DialogueID = 116,
                }
            },
            {115,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: That would be a dream come true.",
                    option2DialogueID = 116,
                }
            },
            {116, //Cafe date ending //150
                new DialogueData()
                {
                    dialogueText = $"What an excellent day.",
                    option2DialogueID = 150,
                }
            },
            {117,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Oh, it's just a pin I got from the aquarium. I thought it was cute so I bought it.",
                    option1Text = "Ask more about it",
                    option1DialogueID = 1180,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 1,
                    },
                    option2Text = "Yeah",
                    option2DialogueID = 119,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -3,
                    },
                }
            },
            {1180,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: This pin seems important to you. What is the story behind it?",
                    option2DialogueID = 118,

                }
            },
            {118,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: When I was little, I'd always say that when I grew up, I wanted to be a penguin. They just seemed so cute and friendly, and I wanted friends.",
                    option2DialogueID = 91,
                }
            },
            {119,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Wait, really? That's adorable.",
                    option2DialogueID = 120,
                }
            },
            {120,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Haha, yeah...",
                    option2DialogueID = 121,
                }
            },
            {121,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ...",
                    option2DialogueID = 122,
                }
            },
            {122,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ...",
                    option1Text = "Be Smooth",
                    option1DialogueID = 125,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -5,
                    },
                    option2Text = "Window",
                    option2DialogueID = 123,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                }
            },
            {123,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Uhhhhh... I have to go to the bathroom!",
                    option2DialogueID = 124,
                }
            },
            {124, //GTFO ending
                new DialogueData()
                {
                    dialogueText = $"{_brokenWindow}! I hit the ground running. A perfect escape.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {125,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You smell different today, is that a new shampoo?",
                    option2DialogueID = 126,
                }
            },
            {126,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Uh, thanks? I don't... think I changed anything.",
                    option2DialogueID = 127,
                }
            },
            {127,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It smells like sweets, just like your personality",
                    option2DialogueID = 128,
                }
            },
            {128,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.awkwardMomentWithTrixie ?
                    $"{_trixieColoredName}: That's... wow. Yikes.": // cringe
                    $"{_trixieColoredName}: *chuckle* That's so... cringe, {_sodaColoredName}. Your game needs work.", //fine
                    option2DialogueID = 129,
                }
            },
            {129,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's getting pretty late. I should head home.",
                    option1Text = "Let's meet again",
                    option1DialogueID =  130,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 1,
                    },
                    option2Text = "Walk her home",
                    option2DialogueID = 150,
                    option2Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                }
            },
            {130,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I had a great time today. Do you think we could hang out again some time?",
                    option2DialogueID = (DialogueFlags.awkwardMomentWithTrixie && DialogueFlags.toldTrixieAboutTheSmell) ? 131 : 132,
                }
            },
            {131,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Mmmmaybe... I'll see if I got time with all the practice for the concert.",
                    option2DialogueID = 150,
                }
            },
            {132,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: I had a great time too! I would love to come back here sometimes.",
                    option2DialogueID = 150,
                }
            },
            {133,
                new DialogueData()
                {
                    dialogueText = $"I nervously shove an entire penguin cookie in my mouth.",
                    option2DialogueID = 104,
                }
            },
            {134,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: No thanks, {_sodaColoredName}. I'm not feeling very hungry.",
                    option1Text = "Not hungry anymore",
                    option1DialogueID = 135,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                    option2Text = "Eat her cookie too",
                    option2DialogueID = 137,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -2,
                    },
                }
            },
            {135,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: No thanks. I don't even think I can finish mine.",
                    option2DialogueID = 136,
                }
            },
            {136,
                new DialogueData()
                {
                    dialogueText = $"It's a really good cookie, but now I feel kinda bad. Does she really want to be here with me?",
                    option2DialogueID = 104,
                }
            },
            {137,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh, sweet! Mind if I snag yours, too?",
                    option2DialogueID = 138,
                }
            },
            {138,
                new DialogueData()
                {
                    dialogueText = $"I eat mine, and follow up with hers, which she cedes with a shrug.",
                    option2DialogueID = 104,
                }
            },
            {139,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: So, the night of my first competition, I was at a party, and was dared to wear a clown suit...",
                    option2DialogueID = 140,
                }
            },
            {140,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Oh my goodness! You mean the night before, right?",
                    option2DialogueID = 141,
                }
            },
            {141,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ...",
                    option2DialogueID = 142,
                }
            },
            {142,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: The party was the night before... right?",
                    option2DialogueID = 143,
                }
            },
            {143,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It may have started one hour before call time...",
                    option2DialogueID = 144,
                }
            },
            {144,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: But surely your concert attire was...?",
                    option2DialogueID = 145,
                }
            },
            {145,
                new DialogueData()
                {
                    dialogueText = $"I shake my head sadly.",
                    option2DialogueID = 146,
                }
            },
            {146,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ...",
                    option2DialogueID = 147,
                }
            },
            {147,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ...and now I don't get nervous anymore!",
                    option2DialogueID = DialogueFlags.trixieAteACookie ? 148 : 129,
                }
            },
            {148,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ...",
                    option2DialogueID = 149,
                }
            },
            {149,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: This is a really good cookie.",
                    option2DialogueID = 129,
                }
            },
            {150,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's pretty dark outside. I'll walk you home.",
                    option2DialogueID = (DialogueFlags.awkwardMomentWithTrixie && DialogueFlags.toldTrixieAboutTheSmell) ? 151:162,
                }
            },
            {151,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thanks for the offer, but my house isn't that far from here.",
                    option1Text = "Insist",
                    option1DialogueID =  152,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 1,
                    },
                    option2Text = "Let her go",
                    option2DialogueID = 161,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                }
            },
            {152,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I insist. I'm worried that someone will kidnap you.",
                    option2DialogueID = 153,
                }
            },
            {153,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: What the hell? {_sodaColoredName}, I can walk home on my own.",
                    option1Text = "Insist more",
                    option1DialogueID =  154,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -3,
                    },
                    option2Text = "Let her go",
                    option2DialogueID = 161,
                    option2Score = new ScoreData()
                    {
                        trixieScore = 1,
                    },
                }
            },
            {154,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: PLEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEASE",
                    option2DialogueID = 155,
                }
            },
            {155, //Transition to night street image
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Babi, you're weird.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {156,
                new DialogueData()
                {
                    dialogueText = $"After a well-deserved total of zero conversation, we arrive at Trixie's house.",
                    option2DialogueID = 157,
                }
            },
            {157,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That was a nice walk.",
                    option2DialogueID = 158,
                }
            },
            {158,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Yeah...",
                    option2DialogueID = 159,
                }
            },
            {159,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, I'll see you tomorrow {_trixieColoredName}!",
                    option2DialogueID = 160,
                }
            },
            {160, //TODO: SLAP -> $"{_trixieColoredName}: That's for the cookie."
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Oh, and Soda?",
                    option2Text ="",
                    option2DialogueID = 0,
                }
            },
            {161,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Alright, but please be careful. See you tomorrow for school!",
                    option2DialogueID = 160,
                }
            },
            {162,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Sure, I could use the company. The walk back home from here is a little boring.",
                    option2DialogueID = 163,
                }
            },
            {163, //Transition to night street image
                new DialogueData()
                {
                    dialogueText = $" With that, we head on our way.",
                    option2Text ="",
                    option2DialogueID = 0,
                }
            },
            {164, //Transition to night street image
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Today was fun. I enjoyed our time, {_trixieColoredName}!",
                    option2DialogueID = 165,
                }
            },
            {165,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: It really was. Maybe next time we can go to the aquarium?",
                    option1Text = "Sure",
                    option1DialogueID =  166,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                    option2Text = "Would love but...",
                    option2DialogueID = 169,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                }
            },
            {166,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I would love to go to the aquarium! Maybe you could play your trombone for the penguins there.",
                    option2DialogueID = 167,
                }
            },
            {167,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Haha, Maybe, I don't know if the aquarium staff would appreciate that though.",
                    option2DialogueID = 168,
                }
            },
            {168,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Maybe, maybe not.",
                    option2DialogueID = 171,
                }
            },
            {169,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I would love to, but I'm more of an amusement park guy myself.",
                    option2DialogueID = 170,
                }
            },
            {170,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: O-Oh, That's alright.",
                    option2DialogueID = 171,
                }
            },
            {171, //Transition to trixie house night
                new DialogueData()
                {
                    dialogueText = $"Before I know it, we've arrived.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {172,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: That was a great trip! We should do this more often.",
                    option2DialogueID = 173,
                }
            },
            {173,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Absolutely, I had a fun time too.",
                    option1Text = DialogueFlags.kissedSomeone ? "":"Loiter",
                    option1DialogueID =  1772,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 25,
                        appaloosaScore = -50,
                        beezerlyScore = -50,
                        kaizyleScore = -50,
                    },
                    option2Text = "Part ways",
                    option2DialogueID = 174,
                }
            },
            {174,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, it's getting late, I should head home myself.",
                    option2DialogueID = 175,
                }
            },
            {175,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Have a good night {_sodaColoredName}!",
                    option2DialogueID = 176,
                }
            },
            {176, //Sweet ending
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Goodnight {_trixieColoredName}. See you tomorrow!",
                    option2DialogueID = 179,
                }
            },
            {1772,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Soda, wait!",
                    option2DialogueID = 177
                }
            },
            {177,
                new DialogueData()
                {
                    dialogueText = $"",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {178,
                new DialogueData()
                {
                    dialogueText = $"W O W Z A .",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {179, //blushes and looks excited
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ... Thanks for lovely night {_sodaColoredName}, I'll see you tomorrow!",
                    option2DialogueID = 180,
                }
            },
            {180, // Happy ending
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} starts heading back to his place]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            #endregion

            #region Beezerly Date
            {320001,
                new DialogueData()
                {
                    dialogueText = $"The next day, I ran into Beezerly after school.",
                    option2DialogueID = 320002,
                }
            },
            {320002,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Hey {_sodaColoredName}, I was thinking about checking out the new hard rock cafe that just opened up. Would you like to go with me?",
                    option2DialogueID = DialogueFlags.talkedShitAboutRock ? 320003 : 320005,
                }
            },
            {320003,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You're really serious about this rock thing?",
                    option2DialogueID = 320004,
                }
            },
            {320004, //TODO: Slap
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Babi almighty, you're such a turd.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {320005,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Hmm, a hard rock cafe? Sure, I'll go. Sounds like a good time!",
                    option2DialogueID = 320006,
                }
            },
            {320006, //Transition to hard rock cafe
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Awesome! I'm really looking forward to it.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {320007,
                new DialogueData()
                {
                    dialogueText = $"We're here, and Beezerly is ecstatic.",
                    option2DialogueID = 320008,
                }
            },
            {320008,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Now this is what I call a good time! I love the energy here.",
                    option2DialogueID = 320009,
                }
            },
            {320009,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Wow, it's definitely a unique experience.",
                    option1Text = "Order food",
                    option1DialogueID = 320100,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },
                    option2Text = "First time here",
                    option2DialogueID = 320010,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 1,
                    },
                }
            },
            {320010,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I've never been to a place like this before. Have you?",
                    option2DialogueID = 320011,
                }
            },
            {320011,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: A couple of times. I love their burgers and fries. And of course, the music is always on point.",
                    option2DialogueID = 320012,
                }
            },
            {320012,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Since its your first time, then we have to make the most of it!",
                    option1Text = "Order food",
                    option1DialogueID = 320100,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },
                    option2Text = "Listen To The Band",
                    option2DialogueID = 320200,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 1,
                    },
                }
            },
            {320200,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: This band is really good! Let's try to get a closer seat.",
                    option2DialogueID = 320201,

                }
            },
            {320201,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Sure!",
                    option2DialogueID = 320202,

                }
            },
            {320202, //Transition to another scene?
                new DialogueData()
                {
                    dialogueText = $"The waiter has no problem finding us a spot.",
                    option2Text = "",
                    option2DialogueID = 0,

                }
            },
            {320203,
                new DialogueData()
                {
                    dialogueText = $"Band: And for our final song, we will take requests from our audience!",
                    option1Text = "Wait",
                    option1DialogueID = 3202040,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = -3,
                    },
                    option2Text = "Request",
                    option2DialogueID = 3202050,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },

                }
            },
            {3202040,
                new DialogueData()
                {
                    dialogueText = $"Band: No request? Then we'll hit you with one last classic. The last song will be Rock 'n' Bone!",
                    option2DialogueID = 3202041,

                }
            },
            {3202041,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: This is my favorite song! I'm so excited for this.", //widepeepoBeezerly
                    option2DialogueID = 3202042,

                }
            },
            {3202042,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: No way! It's mine too!",
                    option2DialogueID = 3202043,

                }
            },
            {3202043,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: You're kidding!",
                    option2DialogueID = 3202044,

                }
            },
            {3202044,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Isn't it everyone's?",
                    option2DialogueID = 3202045,

                }
            },
            {3202045,
                new DialogueData()
                {
                    dialogueText = $"The rest of the song is spent happily discussing the music and our various tastes. We barely notice as the song ends.", //BeezerlyJam and SodaJam
                    option2DialogueID = 320100,

                }
            },
            {3202050,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName} Screams: ROCK 'N' BONE!!!!!",
                    option2DialogueID = 3202051,

                }
            },
            {3202051,
                new DialogueData()
                {
                    dialogueText = $"Band: Sure little one. Let's play Rock 'n' Bone for our man.",
                    option2DialogueID = 3202052,

                }
            },
            {3202052,
                new DialogueData()
                {

                    dialogueText = $"{_beezerlyColoredName}: No way. This is my favorite song!!", //BeezerlyOverlminglyHappy
                    option2DialogueID = 3202053,

                }
            },
            {3202053,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You're kidding!",
                    option2DialogueID = 32020541,

                }
            },
            {32020541,
                new DialogueData()
                {
                    dialogueText = $"Without warning, Beezerly pushes back her chair and shoots up.",
                    option2DialogueID = 3202054,

                }
            },
            {3202054,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: We HAVE to jam to this one.",
                    option1Text = "Follow her",
                    option1DialogueID = 3202055,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 3,
                    },
                    option2Text = "Don't know how to dance",
                    option2DialogueID = 3203055,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = -5,
                    },

                }
            },
            {3202055,
                new DialogueData()
                {
                    dialogueText = $"We dance for the entire song.",
                    option2DialogueID = 3202056,

                }
            },
            {3202056,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I didn't know you could dance, {_sodaColoredName}! That was groovy!",
                    option1Text = "Compliment her",
                    option1DialogueID = 3202057,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },
                    option2Text = "Approach her",
                    option2DialogueID = 3204057,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 1,
                    },

                }
            },
            {3202057,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You either, Beezerly! I like your moves!",
                    option2DialogueID = 3205059,

                }
            },
            {3203055,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'll sit this one out; I don't really know how to dance.",
                    option2DialogueID = 3203056,

                }
            },
            {3203056,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: ... alright. I'll see you in a bit!",
                    option2DialogueID = 3203057,

                }
            },
            {3203057, // transition to 3203058
                new DialogueData()
                {
                    dialogueText = $"Everyone somehow makes space for her wild choreography as she rocks out on the dance floor.",
                    option2Text = "",
                    option2DialogueID = 0,

                }
            },
            {3203058,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: That was exhausting! I need some fresh air, let's go outside.",
                    option2DialogueID = 3203059,

                }
            },
            {3203059, // transition to 3204060
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Sure! Some fresh air would be nice.",
                    option2Text = "",
                    option2DialogueID = 0,

                }
            },
            {3204057,
                new DialogueData()
                {
                    dialogueText = $"I approach her slowly. We're both out of breath.",
                    option2DialogueID = 3204058,

                }
            },
            {3204058,
                new DialogueData()
                {
                    dialogueText = $"We lock eyes for what seems like an eternity.",
                    option1Text = DialogueFlags.kissedSomeone ? "" : "Kiss",
                    option1DialogueID = 3204059,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -50,
                        appaloosaScore = 25,
                        beezerlyScore = -50,
                        kaizyleScore = -50,
                    },
                    option2Text = "Get some fresh air",
                    option2DialogueID = 3205059,
                }
            },
            {3204059,
                new DialogueData()
                {
                    dialogueText = $"",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {3204159, // Transition to 3204060
                new DialogueData()
                {
                    dialogueText = $"If not now, then when?",
                    option2Text = "",
                    option2DialogueID = 0,

                }
            },
            {3205059, // Transition to 3204060
                new DialogueData()
                {
                    dialogueText = $"Exhausted, I head for the door, gesturing for her to follow.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {3204060,
                new DialogueData()
                {
                    dialogueText = "Much later...",
                    option2DialogueID = 3204061,
                }
            },
            {3204061,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Tonight was a blast. We should definitely go there again sometime.",
                    option2DialogueID = 3204062,
                }
            },
            {3204062,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Absolutely! I would love to go there another time and try the food.",
                    option2DialogueID = 3204063,
                }
            },
            {3204063,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Sure thing! I'll catch you tomorrow, {_sodaColoredName}.",
                    option2DialogueID = 3204064,
                }
            },
            {3204064,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: See you tomorrow, {_beezerlyColoredName}.",
                    option2DialogueID = 3204065,
                }
            },
            {3204065, //transition to next chapter
                new DialogueData()
                {
                    dialogueText = $"I head home, Rock 'n' Bone still echoing in my head.",
                    option2Text = "",
                    option2DialogueID = 0,

                }
            },
            {320100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'm getting hungry. Do you want to order?",
                    option2DialogueID = 320101,

                }
            },
            {320101,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Hell yeah! What are you thinking?",
                    option1Text = "Burger",
                    option1DialogueID = 320102,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },
                    option2Text = "Hot dog pizza",
                    option2DialogueID = 320300,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = -4,
                    },

                }
            },
            {320102,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Let's try those burgers and fries{(DialogueFlags.askedIfFirstTime ? " you mentioned earlier!" : "")}.",
                    option2DialogueID = 320013,

                }
            },
            {320300,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh boy! I've never tried hot dogs on pizza before! Let's have that.",
                    option2DialogueID = 320013,

                }
            },
            {320013,
                new DialogueData()
                {
                    dialogueText = $"We order {(DialogueFlags.orderedBurger ? "burgers" : "hot dog pizza")} and drinks and settle down to eat.",
                    option1Text = "Ask more about her",
                    option1DialogueID = 320014,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },
                    option2Text = "Ask about the food",
                    option2DialogueID = 320400,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = DialogueFlags.orderedBurger ? 2 : -3,
                    },
                }
            },
            {320400,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: How do you like the {(DialogueFlags.orderedBurger ? "burgers" : "hot dog pizza")}?",
                    option2DialogueID = DialogueFlags.orderedBurger ? 320401 : 320500,
                }
            },
            {320401,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: The burgers are really amazing. Thanks for ordering for us {_sodaColoredName}.",
                    option2DialogueID = 320014
                }
            },
            {320500,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: ... hot dog, huh. You sure are serious about tromboning.",
                    option2DialogueID = 320014
                }
            },
            {320014,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I know you're not really into the music competition, but I've always wondered why. Is it because you don't think you can win?",
                    option2DialogueID = 320015,
                }
            },
            {320015,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Nah, it's not really about winning or losing for me. I just don't like the idea of competing against other musicians.",
                    option2DialogueID = 320016,
                }
            },
            {320016,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Music is supposed to bring people together, not tear them apart.",
                    option1Text = "Disagree",
                    option1DialogueID = DialogueFlags.isCompetitive ? 320700 : 320800,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = DialogueFlags.isCompetitive ? -7 : 3,
                    },
                    option2Text = "Agree",
                    option2DialogueID = DialogueFlags.isCompetitive ? 320017 : 320600,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore =  2,
                    },
                }
            },
            {320700,
                new DialogueData() //Competitive disagree
                {
                    dialogueText = $"{_sodaColoredName}: There's no point in playing if you're not trying to be the best.",
                    option2DialogueID = 320701
                }
            },
            {320701,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I didn't expect you to be so serious, {_sodaColoredName}.",
                    option2DialogueID = 320022
                }
            },
            {320800,
                new DialogueData() // Casual disagree
                {
                    dialogueText = $"{_sodaColoredName}: Musicians can compete without hating each other.",
                    option2DialogueID = 320801
                }
            },
            {320801,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: A friendly rivalry can help people push their limit without tearing them apart.",
                    option2DialogueID = 320802
                }
            },
            {320802,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I... huh. y'know, Soda, I never thought about it that way.",
                    option2DialogueID = 320019
                }
            },
            {320017,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I see what you mean. For me, the competition is more about pushing myself to be the best I can be. I don't mind losing but winning is also fun.",
                    option2DialogueID = 320018,
                }
            },
            {320600,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I see what you mean. For me, the competition is more about pushing myself to be the best I can be. I'm not trying to beat anyone else, just my own limitations.",
                    option2DialogueID = 320018,
                }
            },
            {320018,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I can respect that. You're really dedicated to the craft.",
                    option2DialogueID = 320019,
                }
            },
            {320019,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I just really love playing the trombone. And I can tell you love music too, just in a different way.",
                    option2DialogueID = 320020,
                }
            },
            {320020,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Absolutely. I love the energy of a crowd at a concert, and the feeling of performing live!",
                    option2DialogueID = 320021,
                }
            },
            {320021,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: And playing my trombone in a more relaxed, casual setting.",
                    option2DialogueID = 320022,
                }
            },
            {320022,
                new DialogueData()
                {
                    dialogueText = $"Holy Wow the food arrived super fast! I was super hungry.",
                    option1Text = DialogueFlags.orderedBurger ? "Discuss Burger" : "Discuss Pizza",
                    option1DialogueID = 321000,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore =  -2,
                    },
                    option2Text = DialogueFlags.orderedBurger ? "Compliment Burger" : "Compliment Pizza",
                    option2DialogueID = 320023,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 1,
                    },
                }
            },
            {321000,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.orderedBurger ? $"{_sodaColoredName}: The burger is alright, but I’ve had better." : $"{_sodaColoredName}:... I'm so sorry. This pizza was a mistake.", 
                    option2DialogueID = DialogueFlags.orderedBurger ? 321001 : 321900
                }
            },
            {321900,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}:... yep.",
                    option2DialogueID = 320025,
                }
            },
            {321001,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Oh? And where would better food be found?",
                    option2DialogueID = 321002
                }
            },
            {321002,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hmm.. I was thinking {_tromBurgerChampName}",
                    option2DialogueID = 321003
                }
            },
            {321003,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: ...", // displeased
                    option2DialogueID = 321004
                }
            },
            {321004,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Seriously? That place?",
                    option1Text = "Kidding!",
                    option1DialogueID = 3210011,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore =  1,
                    },
                    option2Text = "Seriously.",
                    option2DialogueID = 321005,
                    option2Score = new ScoreData()
                    {
                        beezerlyScore =  -4,
                    },
                }
            },
            {321005,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Yeah, they're honestly delicious!",
                    option2DialogueID = 321006
                }
            },
            {321006,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I really like their Wah-Wah-Whopper!",
                    option2DialogueID = 321007
                }
            },
            {321007,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I see, I'd have to try them to know!",
                    option2DialogueID = 320025
                }
            },
            {3210011,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'm kidding, I like this place!",
                    option2DialogueID = 3210012
                }
            },
            {3210012,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: You're a funny guy, {_sodaColoredName}.",
                    option2DialogueID = 320025
                }

            },
            {320023,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.orderedBurger ? $"{_sodaColoredName}: These burgers are amazing. Thanks for suggesting this place." : "This pizza... well, it could definitely be worse.",
                    option2DialogueID = 320024,
                }
            },
            {320923,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Funny, I was still trying to figure out how it could be any worse than it is.",
                    option2DialogueID = 320026,
                }
            },
            {320024,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: No problem. I'm always down for a good burger.",
                    option2DialogueID = 320025,
                }
            },
            {320025,
                new DialogueData()
                {
                    dialogueText = $"We pay for our meals and get ready to go.",
                    option2DialogueID = 320026,
                }
            },
            {320026,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That was a really fun date, {_beezerlyColoredName}. I'm glad we went out tonight.",
                    option2DialogueID = 320027,
                }
            },
            {320027,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Me too, {_sodaColoredName}. You're pretty cool for a competition junkie.",
                    option2DialogueID = 320028,
                }
            },
            {320028,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: And you're pretty cool for a rebellious tromboner.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {320030, // transition to outside scene 320031
                new DialogueData()
                {
                    dialogueText = $"We step outside, enjoying the cool air.",
                    option2DialogueID = 320031,
                }
            },
            {320031,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I would love to come here again. I had a great time.",
                    option2DialogueID = 320032,
                }
            },
            {320032,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: So did I.",
                    option2DialogueID = 3204065,
                }
            },
            #endregion
 
            #region Appaloosa Date
            {330000,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey Appaloosa! I really enjoy your tunes, thanks for letting me learn from you!",
                    option2DialogueID = 330001,
                }
            },
            {330001,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Sure, Soda! There's nothing I love more than a passionate musician!",
                    option2DialogueID = 3300011,
                }
            },
            {3300011,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Hows about a few warm-up exercises?",
                    option2DialogueID = 330002,
                }
            },
            {330002,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Sure thing!",
                    option2DialogueID = 330003
                }
            },
            {330003,
                new DialogueData()
                {
                    dialogueText = $"Holy wow, Appaloosa is teaching me so much I've never seen before!",
                    option2DialogueID = 330004
                }
            },
            {330004,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: You're a quick learner, Soda. You ever played at a jazz bar before?",
                    option2DialogueID = 330005
                }
            },
            {330005,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Nope.",
                    option2DialogueID = 330006
                }
            },
            {330006,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Well, it's your lucky day; I work at one not too far from here!",
                    option2DialogueID = 330007
                }
            },
            {330007,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Wanna go tonight? We can grab a drink and listen to some live music.",
                    option1Text = $"Why not",
                    option1DialogueID = 3310001,
                    option1Score = new ScoreData
                    {
                        appaloosaScore = -1f
                    },
                    option2Text = $"Absolutely",
                    option2DialogueID = 331000,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 3f
                    }
                }
            },
            {3310001, // Choice 2; sure
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I don't see why not.",
                    option2DialogueID = 3310002,
                }
            },
            {331000, // Choice 1; yes
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That sounds amazing! I'd love to.", // SodaAgree
                    option2DialogueID = 33100020,
                }
            },
            {33100020,
                new DialogueData() 
                {
                    dialogueText = $"{_appaloosaColoredName}: Great! I'll pick you up after school.",
                    option2DialogueID = 331000201
                } 
            },
            {331000201,
                new DialogueData() 
                {
                    dialogueText = $"As I pack up, I wonder what it is she's looking for from me.",
                    option2DialogueID = 331000202
                } 
            },
            {331000202,
                new DialogueData() 
                {
                    dialogueText = $"She's my tutor, and I'm her student.",
                    option1Text = "I should drop it.",
                    option1DialogueID = 33101021,
                    option2Text = "That's the best part.",
                    option2DialogueID = 33100021,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 1f
                    },
                } 
            },
            {33100021,
                new DialogueData()
                {
                    dialogueText = "I hear a knock on the door and brace myself as I nudge it open.",
                    option2DialogueID = 33100023,
                }
            },
            {33101021,
                new DialogueData()
                {
                    dialogueText = "...",
                    option2DialogueID = 33100023,
                }
            },
            {33100023,
                new DialogueData() 
                {
                    dialogueText = $"I meet her outside, the hellish cacophony of metal on metal still ringing in my ears, and together we walk down the road.",
                    option2DialogueID = 3310002
                } 
            },
            {3310002,
                new DialogueData()
                {
                    dialogueText = $"She starts up her autotrombile and I hop in.",
                    option2Text = "",
                    option2DialogueID = 0
                }
            },
            {331001,
                new DialogueData()
                {
                    dialogueText = $"Pulling up to the curb, I'm surprised to find that the venue looks small and quiet from the outside.",
                    option2DialogueID = 3310092
                }
            },
            {3310092,
                new DialogueData() 
                {
                    dialogueText = $"Only the occasional person passes through the doorway, but stepping inside, the room is well-packed.",
                    option2DialogueID = 3310093
                } 
            },
            {3310093,
                new DialogueData() 
                {
                    dialogueText = $"At that point however, I hear the music, and understand.",
                    option2DialogueID = 3310094
                } 
            },
            {3310094,
                new DialogueData() 
                {
                    dialogueText = $"What tromboner in their right mind would leave a sound like this?",
                    option2DialogueID = 331002
                } 
            },
            {331002,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Whadd'ya think, {_sodaColoredName}? This is the real deal, yeah?", // Yelling over music
                    option1DialogueID = 331200,
                    option1Text = $"Not terrible...",
                    option1Score = new ScoreData
                    {
                        appaloosaScore = -2f
                    },
                    option2DialogueID = 331100,
                    option2Text = $"Absotootely!",
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 3f
                    }
                }
            },
            {331200, // Choice 2; unimpressed
                new DialogueData // FLAG unimpressedAppaloosa
                {
                    dialogueText = $"{_sodaColoredName}: Y'know, it could be worse.",
                    option2DialogueID = 331104
                }
            },
            {331100, //Choice 1; yes
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Definitely. I can feel the energy in this place!",
                    option2DialogueID = 331101
                }
            },
            {331101,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: That's jazz for ya! It brings everyone together in one family!",
                    option2DialogueID = 331102
                }
            },
            {331102,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That's pretty neat!",
                    option2DialogueID = 331103
                }
            },
            {331103,
                new DialogueData()
                {

                    dialogueText = $"{_appaloosaColoredName}: That's what I'm here for. To share my love of jazz with others.",
                    option1Text = DialogueFlags.obsessAppaloosa ? "Flirt" : "Advice",
                    option1DialogueID = 3311031,
                    option1Score = new ScoreData
                    {
                        appaloosaScore = 2f
                    },
                    option2Text = "Drinks",
                    option2DialogueID = 331104,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = -1f
                    },
                }
            },
            {3311031,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.obsessAppaloosa ? $"{_sodaColoredName}: I thought you were here for your passionate student?" : $"{_sodaColoredName}: Speaking of, how do tromboners create complex chords in a group like that?",
                    option2DialogueID = 3311032
                }
            },
            {3311032,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.obsessAppaloosa ? $"{_appaloosaColoredName}: Oh, stop it!" : $"{_appaloosaColoredName}: Now, that's interesting, because you have to understand that harmony is more than just major and minor.",

                    option2DialogueID = 3311033
                }
            },
            {3311033,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Let's order and have a seat. They put on great shows here!",
                    option2DialogueID = 331104
                }
            },
            {331104,
                new DialogueData()
                {
                    dialogueText = $"We order our drinks and take a seat, letting the music wash over us.",
                    option2DialogueID = 331220
                }
            },
            {331220, 
                new DialogueData()
                {
                    dialogueText = DialogueFlags.unimpressedAppaloosa ? (DialogueFlags.obsessAppaloosa ? $"{_sodaColoredName}: So, do you come here often..?" : $"{_appaloosaColoredName}: Think of chords as sentences, where the complexity of an idea lies in structure and vocabulary.") : $"{_appaloosaColoredName}: So, where do you want to go with tromboning?",
                    option2DialogueID = 331105
                }
            },
            {331105,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.unimpressedAppaloosa ? (DialogueFlags.obsessAppaloosa ?
                    $"{_sodaColoredName}: This place is impressive, {_appaloosaColoredName}. I can't thank you enough for bringing me here." : // if appaloosa unimpressed
                    $"{_sodaColoredName}: So words are just... notes?") : // if appaloosa is impressed and soda is obsess
                    (DialogueFlags.isCompetitive ? $"{_sodaColoredName}: I'm definitely going pro once I graduate." : // if appaloosa is impressed, soda not impressed but soda is competitive
                    $"{_sodaColoredName}: I think I just started tromboning because I come from a long line of tromboners, but doesn't everyone?"), // SodaEat
                    option2DialogueID = 331106
                }
            },
            {331106,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.unimpressedAppaloosa ? (DialogueFlags.obsessAppaloosa ? $"{_appaloosaColoredName}: It's my pleasure, {_sodaColoredName}. You remind me of myself when I was your age. So full of passion and potential." : $"{_appaloosaColoredName}: Exactly! And where you choose to, say, place the root of the chord down the octave determines what you communicate to the listener!") : (DialogueFlags.isCompetitive ? $"{_appaloosaColoredName}: That's great! You remind me of the tromboning drive I had when I first started." : $"{_appaloosaColoredName}: You're definitely not alone there. It's a big trombiverse out there, and all we can do is our best! So don't lose hope."),
                    option1Text = $"Thank",
                    option1DialogueID = 331120,
                    option1Score = new ScoreData
                    {
                        appaloosaScore = 1f
                    },
                    option2Text = DialogueFlags.obsessAppaloosa ? "Flirt" : "",
                    option2DialogueID = 33110,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 3f
                    }
                }
            },
            {331120, // Choice 2; thank
                new DialogueData()
                {
                    dialogueText = DialogueFlags.obsessAppaloosa ? $"{_sodaColoredName}: Oh, um... thanks." : (DialogueFlags.unimpressedAppaloosa ? $"{_sodaColoredName}: That's so cool! Tell me more." : $"{_sodaColoredName}: Thanks, {_appaloosaColoredName}. That... that helps."),
                    option2DialogueID = 331113
                }
            },
            {33110, // Choice 1; flirt
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Thanks, {_appaloosaColoredName}. Means a lot coming from someone with your talent.", // SodaEmbarrassedLight
                    option2DialogueID = 331111

                }
            },
            {331111,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Oh shush, {_sodaColoredName}. If you ever wanna perform here, though, I can help you start.", // Laugh
                    option2DialogueID = 331112
                }
            },
            {331112,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'll consider that offer, thanks!", // SodaWOW
                    option2DialogueID = 331113
                }
            },
            {331113,
                new DialogueData()
                {
                    dialogueText = $"As the night wears on, {(DialogueFlags.obsessAppaloosa ? $"{_appaloosaColoredName} loses herself" : "we lose ourselves")} in the music, the moment.",
                    option2DialogueID = DialogueFlags.unimpressedAppaloosa ? 3311131 : 331114
                }
            },
            {3311131,
                new DialogueData() 
                {
                    dialogueText = $"It's interesting in places, sure, but {(DialogueFlags.isCompetitive ? "seeing all this tooting just makes me want to get home and practice." : "I know why I really came here.")}",
                    option2DialogueID = 331114
                }
            },
            {331114,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: This has been {(DialogueFlags.unimpressedAppaloosa ? "an interesting" : (DialogueFlags.flirtAppaloosa ? "such an incredible" : "a good"))} night, {_appaloosaColoredName}. {(!(DialogueFlags.unimpressedAppaloosa) ? "Thank you again for everything." : "")}",
                    option1Text  = DialogueFlags.kissedSomeone || !DialogueFlags.obsessAppaloosa ? "":"Kiss",
                    option1DialogueID = DialogueFlags.flirtAppaloosa ? 3311141 : 3311142,
                    option1Score = new ScoreData
                    {
                        appaloosaScore = DialogueFlags.flirtAppaloosa ? 50f : -10f,
                        beezerlyScore = DialogueFlags.flirtAppaloosa ? -25f : 0f,
                        trixieScore = DialogueFlags.flirtAppaloosa ? -25f : 0f,
                        kaizyleScore = DialogueFlags.flirtAppaloosa ? -25f : 0f,
                    },
                    option2Text = "Head home",
                    option2DialogueID = 331115,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 2f
                    }
                }
            },
            {3311141,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: ...",
                    option2DialogueID = 3311142
                }

            },
            {3311142,
                new DialogueData()
                {
                    dialogueText = $"I lean in.", //moment of truth
                    option2DialogueID = DialogueFlags.unimpressedAppaloosa ? 331142 : 3311150,
                }
            },
            {331142,
                new DialogueData()
                {
                    dialogueText = $"She leans away.", // SET FLAG awkwardAppaloosa TODO
                    option2DialogueID = 331115
                }
            },
            {3311150,
                new DialogueData() {
                    dialogueText = "", // SET FLAG kissedAppaloosa TODO
                    option2DialogueID = 0
                }
            },
            {3311151, //transition back to 331115
                new DialogueData() {
                    dialogueText = "", 
                    option2DialogueID = 0
                }
            },
            {331115,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: {(!DialogueFlags.awkwardAppaloosa ? "" : "Uhmm...")} Anytime, {_sodaColoredName}.", // If kissed Appaloosa: SodaEmbarrasedLight ; If awkwardAppaloosa: SodaPlead
                    option2DialogueID = 331116

                }
            },
            {331116,
                new DialogueData()
                {
                    dialogueText = $"That was a very fun date.",
                    option2Text = "",
                    option2DialogueID = 0

                }
            },
            #endregion
 
            #region Kaizyle Date
            {340000,
                new DialogueData()
                {
                    dialogueText = $"It's the end of trombone theory, and everyone, including Kaizyle, is packing their bags. As the rest of the class filters out of the room, I steel myself and approach her desk.",
                    option2DialogueID = 340001,
                }
            },
            {340001,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey Kaizyle, how's it going?",
                    option2DialogueID = 340002,
                }
            },
            {340002,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Busy.",
                    option2DialogueID = 340003,
                }
            },
            {340003,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh, uh... Got any plans for tomorrow?",
                    option2DialogueID = 3400031,
                }
            },
            {3400031,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yes. What are you getting at?",
                    option2DialogueID = 3400032,
                }
            },
            {3400032,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I was hoping we could go to the park together.",
                    option2DialogueID = 3400033,
                }
            },
            {3400033,
                new DialogueData()
                {
                    dialogueText = $"Her silence is even more threatening than her tone of voice.",
                    option2DialogueID = 3400034,
                }
            },
            {3400034,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well uh I mean well you see and it's ok if you don't want to I don't know why I would either um wait that came out wrong because I mean I would obviously that's why I asked but if you don't want to that's okay too because it was just a stupid idea after all and I don't know why I just brazenly assumed you were okay with it even though that's not really what I meant by asking but I want to make it clear that wait this isn't very clear I'm sorry I don't know why I asked at all this was dumb of me I'm sorry for wasting your time I should just go to my class now and leave you alone",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {3400035,
                new DialogueData()
                {
                    dialogueText = $"She cuts me off. Thank Babi.",
                    option2DialogueID = 340004,
                }
            },
            {340004,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Why?",
                    option1Text = "As a date",
                    option1DialogueID = 340100,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = -3f
                    },
                    option2Text = "Literally anything else",
                    option2DialogueID = 340005,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = 2f
                    },
                }
            },
            {340100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I was hoping it could be sort of like a date.",
                    option2DialogueID = 340101,
                }
            },
            {340101,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: With you? No.",
                    option2DialogueID = 340102,
                }
            },
            {340102, //GTFO ending
                new DialogueData()
                {
                    dialogueText = $"Nice going, champ.",
                    option2DialogueID = 350000
                }
            },
            {340005,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Just as a friendly get-together. We don't know each other that well, and I thought I'd extend an olive branch.",
                    option2DialogueID = 340006,
                }
            },
            {340006,
                new DialogueData()
                {
                    dialogueText = $"I didn't know nervous cartoon gulps were a real thing. Neat.",
                    option2DialogueID = 340007,
                }
            },
            {340007,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ... in the spirit of musicianship.",
                    option2DialogueID = 340008,
                }
            },
            {340008,
                new DialogueData()
                {
                    dialogueText = $"She stares at me for a long moment.",
                    option2DialogueID = 340009,
                }
            },
            {340009,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Okay.",
                    option2DialogueID = 340010,
                }
            },
            {340010,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Right, sorry, I'll get out of your- wait, really?",
                    option2DialogueID = 340011,
                }
            },
            {340011,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I don't see why not. Downtown tomorrow, an hour after school. If you're not there, I leave.",
                    option2DialogueID = 340012,
                }
            },
            {340012,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh, uh, that's great! See you then! I'm glad that-",
                    option2DialogueID = 340013,
                }
            },
            {340013,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Still busy.",
                    option1Text = "Yippies!",
                    option1DialogueID = 34010810,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = -1f
                    },
                    option2Text = "Cya later",
                    option2DialogueID = 34000810,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = 1f
                    },
                }
            },
            {34010810,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: YIPPIE! I'll see you tomorrow kay kay!",
                    option2DialogueID = 3401081
                }
            },
            {3401081, // TRANSITION TO DOWNTOWN
                new DialogueData()
                {
                    dialogueText = $"She rolls her eyes as she picks up her bag and exits.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {34000810,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ... I'll get out of your hair.",
                    option2DialogueID = 34010811,
                }
            },
            {34010811,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ...",
                    option2DialogueID = 3401081,
                }
            },
            {3400081,// TRANSITION TO DOWNTOWN
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ... thanks.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {3400090,
                new DialogueData()
                {
                    dialogueText = $"I'm almost late; she's gonna leave!",
                    option2DialogueID = 3400091,
                }
            },
            {3400091,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey... I had to run... a little... it's good to see you again. How have you been?",
                    option2DialogueID = 3400101,
                }
            },
            {3400101,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I've been fine.",
                    option2DialogueID = 34000111,
                }
            },
            {34000111,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ...",
                    option2DialogueID = 3400011,
                }
            },
            {3400011,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ...",
                    option2DialogueID = 3400012,
                }
            },
            {3400012,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: So, what are we doing?",
                    option2DialogueID = 3400013,
                }
            },
            {3400013,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I dunno! I just thought it would be nice to get out and enjoy some sunshine. What do you want to do?",
                    option2DialogueID = 3400014,
                }
            },
            {3400014,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: It's up to you.",
                    option1Text = "Glissandogs",
                    option1DialogueID = 340200,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = -3f
                    },
                    option2Text = "Tromb-Cone",
                    option2DialogueID = 340015,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = 3f
                    },
                }
            },
            {340200,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Let's go to Glissandogs!",
                    option2DialogueID = 340201,
                }
            },
            {340201, // KayBrag
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Uh, no. I only eat high quality hot dogs.",
                    option2DialogueID = 340202,
                }
            },
            {340202,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Why only high quality hot dogs specifically?",
                    option2DialogueID = 340203,
                }
            },
            {340203,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: To be the best tromboner, you can only eat the best hot dogs.",
                    option2DialogueID = 340204,
                }
            },
            {340204,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Well, that's how my family has stayed on top of the music scene for decades.", //idk how to word this sheit pliz send help
                    option1Text = "Ask more about her hotdogs",
                    option1DialogueID = 340205,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = 2f
                    },
                    option2Text = "Praise Glissandogs",
                    option2DialogueID = 340305,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = -3f
                    },
                }
            },
            {340305,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: But Glissandogs are my favorite hotdogs!",
                    option2DialogueID = 340306,
                }
            },
            {340306,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: You're an enemy of art and I pity your ignorance.",
                    option2DialogueID = 340307,
                }
            },
            {340307,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Don't flame-broil me like that. Hear me out!",
                    option2DialogueID = 340308,
                }
            },
            {340308,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: If I mustard.",
                    option2DialogueID = 340309,
                }
            },
            {340309,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Be that way. I believe a true tromboner should relish every dog they can get.",
                    option2DialogueID = 340310,
                }
            },
            {340310,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: And you'll be playing ketchup the rest of your career.",
                    option2DialogueID = 340311,
                }
            },
            {340311,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Fine then. I'll be frank. Screw you, Kaizyle.",
                    option2DialogueID = 340312,
                }
            },
            {340312,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: There it is! What's been stopping you? You've been up in my grill this whole time anyway.",
                    option2DialogueID = 340313, 
                }
            },
            {340313,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Only because you're the wurst person I know.",
                    option2DialogueID = 340314,
                }
            },
            {340314,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Great Babi above, you are SUCH a weiner.",
                    option2DialogueID = 340315, // more???
                }
            },
            {340315,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh yeah?",
                    option2DialogueID = 340316,
                }
            },
            {340316, //kaizyleLeaves
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yeah!",
                    option2DialogueID = 340317,
                }
            },
            {340317,
                new DialogueData()
                {
                    dialogueText = $"It's hard not to cry at times like these.",
                    option2DialogueID = 340318,
                }
            },
            {340318, //lets go end of chapter 3 LMFAOOOO
                new DialogueData()
                {
                    dialogueText = $"Mustarding my final ounce of pride, I square my shoulders and walk home.",
                    option2DialogueID = 350000,
                }
            },
            {340205,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Where do you get your hotdogs?",
                    option2DialogueID = 340206,
                }
            },
            {340206,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: We make ours homemade. It's a family recipe.",
                    option1Text = "Interesting",
                    option1DialogueID = 340207,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = 2f
                    },
                    option2Text = "OMG NO WAY!",
                    option2DialogueID = 340407,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = -5f
                    },
                }
            },
            {340407,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That's mega awesome cool!!",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {340408,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: What's in them??",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {340409,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Do you sell them somewhere??",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {340410,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: What do you like to put in your hotdogs??",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {3404101,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Stop.",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {340411,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Do you know how to grill hotdogs??",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {340412,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Can we try different hotdogs from around the world??",
                    option2DialogueID = 0, //Auto Dialogue
                }
            },
            {340413,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: {_sodaColoredName}. STOP.",
                    option2DialogueID = 340208,
                }
            },
            {340207,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hmm, that's interesting. I'd be curious to try them sometimes.",
                    option2DialogueID = 340208,
                }
            },
            {340208,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Anyway, let's go to Tromb-Cone Ice Cream Parlor!",
                    option2DialogueID = 340016,
                }
            },
            {340015,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: How about we go grab some ice cream? There's a really good place down the street.",
                    option2DialogueID = 340016,
                }
            },
            {340016, //TRANSITION TO THE ICECREAM PLACE
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Fine, but only because I won't be able to handle an eleventh hot dog today.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {340017,
                new DialogueData()
                {
                    dialogueText = $"Hmm I'm not sure what I should get...",
                    option1Text = "Ice Cream",
                    option1DialogueID = 340300,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = 1f
                    },
                    option2Text = "Sundae",
                    option2DialogueID = 340400,
                }
            },
            {340300,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'll get an ice cream, what about you?",
                    option2DialogueID = 3400171,
                }
            },
            {340400,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'll get a sundae, what about you?",
                    option2DialogueID = 3400171,
                }
            },
            {3400171,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I'll just get an ice cream{(DialogueFlags.gotIceCream ? " as well.":".")}",
                    option2DialogueID = 340018,
                }
            },
            {340018,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: So, how's your music going? I heard you had a big performance last week.",
                    option2DialogueID = 340020,
                }
            },
            {340020, //Happy or satisfied
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: It went well. I played the solo in the third movement of the Mozart Requiem.",
                    option2DialogueID = 3400201,
                }
            },
            {3400201, //Proud
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: It was a challenge, but I think I nailed it.",
                    option2DialogueID = 340021,
                }
            },
            {340021,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That's awesome! I wish I could have been there to see it.",
                    option2DialogueID = 340022,
                }
            },
            {340022,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Maybe I can show you a video sometime.",
                    option1Text = "Sure",
                    option1DialogueID = 3400221,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = -1f
                    },
                    option2Text = "Compliment",
                    option2DialogueID = 340023,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = 3f
                    },
                }
            },
            {3400221,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Sure, I'd like that.",
                    option2DialogueID = 340025,
                }
            },
            {340023,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I've always admired your talent and dedication to your craft. It's inspiring.",
                    option2DialogueID = 340024,
                }
            },
            {340024,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Thanks, I appreciate that.",
                    option2DialogueID = 340025,
                }
            },
            {340025,
                new DialogueData()
                {
                    dialogueText = $"Holy wow, Kaizyle already finished? I'm only half done!",
                    option1Text = "Throw away",
                    option1DialogueID = 3400261,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = -6f
                    },
                    option2Text = "Keep it",
                    option2DialogueID = 3400262,
                }
            },
            {3400261,
                new DialogueData()
                {
                    dialogueText = $"I think I'm done with the rest. I'll just throw it away.",
                    option2DialogueID = 3400263,
                }
            },
            {3400262,
                new DialogueData()
                {
                    dialogueText = $"I'll bring it with me just in case...",
                    option2DialogueID = 3400263,
                }
            },
            {3400263,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Should we go for a walk?",
                    option2DialogueID = 3400264,
                }
            },
            {3400264, // TRANSITION TO 340026 OTHER DOWNTOWN
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: {(DialogueFlags.gotIceCream ? "Sure.":"Whatever you want Soda.")}",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {340026,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: What kind of music do you listen to?",
                    option2DialogueID = 340027,
                }
            },
            {340027,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Mostly classical, but I also enjoy jazz and some pop. What about you?",
                    option2DialogueID = 340028,
                }
            },
            {340028,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'm pretty open-minded when it comes to music. I like anything that makes me feel something {(DialogueFlags.talkedShitAboutRock ? "as long as it's not rock music." : ", whether it's rock, hip hop, or electronic.")}",
                    option2DialogueID = 340029,
                }
            },
            {340029,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}:This is a lot of exercise in one day... Maybe we should sit somewhere.",
                    option2DialogueID = 340030,
                }
            },
            {340030,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Let's head to that bench over here and sit.",
                    option2DialogueID = 340031,
                }
            },
            {340031,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Sure.",
                    option2DialogueID = 340131,
                }
            },
            {340131,
                new DialogueData()
                {
                    dialogueText = $"Ahhhhhhhh thanks Babi my legs were burning.",
                    option2DialogueID = DialogueFlags.gotIceCream || DialogueFlags.gotSundae ? 340132 : 340032,
                }
            },
            {340132,
                new DialogueData()
                {
                    dialogueText = $"Holy wow, there is no way I can finish that {(DialogueFlags.gotIceCream?"ice cream...":"sunday...")} maybe Kaizyle is gonna want it?",
                    option2DialogueID = 340133,
                }
            },
            {340133,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'm feeling pretty full. Do you want to finish this for me?",
                    option2DialogueID = 340134,
                }
            },
            {340134,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I could definitely eat more ice cream. Thanks Soda.",
                    option2DialogueID = 340135,
                }
            },
            {340135,
                new DialogueData()
                {
                    dialogueText = $"She seem to really like sweet things...",
                    option2DialogueID = 340136,
                }
            },
            {340136, //SodaShy type
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh, I think you have some ice cream on your lips",
                    option2DialogueID = 3401361,
                }
            },
            {3401361,
                new DialogueData()
                {
                    dialogueText = $"Kaizyle pauses, then blushes for some inexplicable reason.",
                    option2DialogueID = 340137,
                }
            },
            {340137,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Oh, really? Could you... show me where?",
                    option1Text = DialogueFlags.kissedSomeone ? "":"Show using your lips",
                    option1DialogueID = 340138,
                    option1Score = new ScoreData
                    {
                        kaizyleScore = 50f,
                        appaloosaScore = -25f,
                        beezerlyScore = -25f,
                        trixieScore = -25f
                    },
                    option2Text = "Show using your finger",
                    option2DialogueID = 340238,
                    option2Score = new ScoreData
                    {
                        kaizyleScore = 2f
                    },
                }
            },
            {340138, // KISSING KAIZYLE SCENE LETS GOOOOOOOOOOOOO
                new DialogueData()
                {
                    dialogueText = $"",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {340139, // transition to 340239
                new DialogueData()
                {
                    dialogueText = $"The ice cream on her lips tasted like ice cream. I dunno, what do want from me?",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {340238, //KayOh
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Here...",
                    option2DialogueID = 340239,
                }
            },
            {340239, // KayExtraBlush or KayFine?
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Oh, thank you Soda.",
                    option2DialogueID = DialogueFlags.kissedKaizyle ? 340240 : 340032,
                }
            },
            {340240,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I can't let good ice cream go to waste.",
                    option2DialogueID = 340241,
                }
            },
            {340241, //Kai shy
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ...",
                    option2DialogueID = 340032,
                }
            },
            {340032,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's been really nice spending time with you today, Kaizyle. I feel like I've gotten to know you better.",
                    option2DialogueID = 340033,
                }
            },
            {340033,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.gotIceCream ?
                    $"{_kaizyleColoredName}: Yeah, it's been {(DialogueFlags.kissedKaizyle?"very ": "")}enjoyable.":
                    $"{_kaizyleColoredName}: Yeah, it's been... interesting.",
                    option2DialogueID = 340034,
                }
            },
            {340034,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You know, I've been wanting to ask you something.",
                    option2DialogueID = 340035,
                }
            },
            {340035,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Would you like to go out with me again sometime? Maybe we could catch a concert or something.",
                    option2DialogueID = 340036,
                }
            },
            {340036,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: You're pretty bold, Soda. I guess I could consider it.",
                    option2DialogueID = 340037,
                }
            },
            {340037,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Sounds good. We should get some rest for tomorrow's competition.",
                    option2DialogueID = 340038,
                }
            },
            {340038,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: You're right, I should head home. See you tomorrow, Soda{(DialogueFlags.kissedKaizyle ? "... take care of yourself":"")}.",
                    option2DialogueID = 340039,
                }
            },
            {340039,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: See you at the competition, Kaizyle!",
                    option2DialogueID = 350000,
                }
            },
            {350000, // FINALLYYYYYY AHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH WE DID IT REDDIT
                new DialogueData()
                {
                    dialogueText = $"[END OF CHAPTER 3]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            #endregion
        };

        public static int GetChapter4FirstCharacterEnter()
        {
            if (!DialogueFlags.awkwardMomentWithTrixie && !DialogueFlags.toldTrixieAboutTheSmell)
                return 410002;
            else if (!DialogueFlags.talkedShitAboutRock)
                return 410003;
            else if (!DialogueFlags.awkwardAppaloosa && !DialogueFlags.unimpressedAppaloosa)
                return 410004;
            else if (!DialogueFlags.arguedAboutGlissandogs)
                return 410005;
            else
                return 4100111;
        }

        public static int GetChapter4SecondCharacterEnter()
        {
            if (!DialogueFlags.talkedShitAboutRock)
                return 410003;
            else if (!DialogueFlags.awkwardAppaloosa && !DialogueFlags.unimpressedAppaloosa)
                return 410004;
            else if (!DialogueFlags.arguedAboutGlissandogs)
                return 410005;
            else
                return 410006;
        }
        public static int GetChapter4ThirdCharacterEnter()
        {
            if (!DialogueFlags.awkwardAppaloosa && !DialogueFlags.unimpressedAppaloosa)
                return 410004;
            else if (!DialogueFlags.arguedAboutGlissandogs)
                return 410005;
            else
                return 410006;
        }
        public static int GetChapter4FourthCharacterEnter()
        {
            if (!DialogueFlags.arguedAboutGlissandogs)
                return 410005;
            else
                return 410006;
        }

        public static bool CanPerformWithTrixie()
        {
            return _scoreData.trixieScore >= 10 && ((DialogueFlags.kissedSomeone && DialogueFlags.kissedTrixie) || !DialogueFlags.kissedSomeone) && DialogueFlags.trixiePresent;
        }
        public static bool CanPerformWithBeezerly()
        {
            return _scoreData.beezerlyScore >= 10 && ((DialogueFlags.kissedSomeone && DialogueFlags.kissedBeezerly) || !DialogueFlags.kissedSomeone) && DialogueFlags.beezerlyPresent;
        }
        public static bool CanPerformWithAppaloosa()
        {
            return _scoreData.appaloosaScore >= 10 && ((DialogueFlags.kissedSomeone && DialogueFlags.appaloosaPresent) || !DialogueFlags.kissedSomeone) && DialogueFlags.appaloosaPresent;
        }
        public static bool CanPerformWithKaizyle()
        {
            return _scoreData.kaizyleScore >= 10 && ((DialogueFlags.kissedSomeone && DialogueFlags.kissedKaizyle) || !DialogueFlags.kissedSomeone) && DialogueFlags.kaizylePresent;
        }

        public static Dictionary<int, DialogueData> GetDialogueChapter4() => new Dictionary<int, DialogueData>()
        {
            #region Chapter 4
            {410000,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 4: Concerto Conundrum",
                    option2DialogueID = 4100001,
                }
            },
            {4100001,
                new DialogueData()
                {
                    dialogueText = $"It's the day of the competition.",
                    option2DialogueID = 4100002,
                }
            },
            {4100002,
                new DialogueData()
                {
                    dialogueText = $"I've been practicing day and night, but that doesn't bother me.",
                    option2DialogueID = 4100003,
                }
            },
            {4100003,
                new DialogueData()
                {
                    dialogueText = $"Tooting is the only thing I ever loved.",
                    option2DialogueID = 4100004,
                }
            },
            {4100004,
                new DialogueData()
                {
                    dialogueText = $"But today...",
                    option2DialogueID = 410001,
                }
            },
            {410001,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Sorry I'm late. *Someone* had to oil those accursed hinges in the music classroom.",
                    option2DialogueID =  GetChapter4FirstCharacterEnter()

                }
            },
            {410002,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.gtfoOfTheDateEarly ? $"{_trixieColoredName}: Oh Babi, I'm so scared. What if I mess up?" : (DialogueFlags.saidTheTruth ? "Trixiebell looks at me and giggles... at least she's not nervous anymore" : "Trixie sits in the corner, breathing slowly like we practiced."),
                    option2DialogueID = GetChapter4SecondCharacterEnter()
                }
            },
            {410003,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Don't worry about it, guys. Just play from the heart and have fun.",
                    option2DialogueID = GetChapter4ThirdCharacterEnter(),
                }
            },
            {410004,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: That's right. It's not about winning or losing, it's about expressing yourself through music.",
                    option2DialogueID = GetChapter4FourthCharacterEnter(),
                }
            },
            {410005,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I have no doubt that we'll win if we stick to the plan. Let's show them what real musicians can do.",
                    option2DialogueID = 410006,
                }
            },
            {410006,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I have to choose which girl to perform with... wait. When was this trope established?",
                    option2DialogueID = CanPerformWithTrixie() ? 410007 :
                                        CanPerformWithBeezerly() ? 410008 :
                                        CanPerformWithAppaloosa() ? 410009 :
                                        CanPerformWithKaizyle() ? 410010 : 4100111,
                }
            },
            {410007,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: {_sodaColoredName}, will you perform with me?",
                    option1Text = "Perform With Trixie",
                    option1DialogueID = 410100,
                    option2Text = "Deny Offer",
                    option2DialogueID = CanPerformWithBeezerly() ? 410008 :
                                        CanPerformWithAppaloosa() ? 410009 :
                                        CanPerformWithKaizyle() ? 410010 : 410011,
                }
            },
            {410100, //transition to loading the song for trixiebell
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} and {_trixieColoredName} are getting ready to perform together]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {410008,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: How about me??",
                    option1Text = "Perform With Beezerly",
                    option1DialogueID = 410200,
                    option2Text = "Deny Offer",
                    option2DialogueID = CanPerformWithAppaloosa() ? 410009 :
                                        CanPerformWithKaizyle() ? 410010 : 410011,
                }
            },
            {410200, //transition to loading the song for Beezerly
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} and {_beezerlyColoredName} are getting ready to perform together]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {410009,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: I'd love to perform with you, {_sodaColoredName}. What about you?",
                    option1Text = "Perform With Appaloosa",
                    option1DialogueID = 410300,
                    option2Text = "Deny Offer",
                    option2DialogueID = CanPerformWithKaizyle() ? 410010 : 410011,
                }
            },
            {410300, //transition to loading the song for Appaloosa
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} and {_appaloosaColoredName} are getting ready to perform together]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {410010,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Hurry up and choose already, {_sodaColoredName}. We don't have all day.",
                    option1Text = "Perform With Kaizyle",
                    option1DialogueID = 410400,
                    option2Text = "Deny Offer",
                    option2DialogueID = CanPerformWithTrixie() &&
                                        CanPerformWithBeezerly() &&
                                        CanPerformWithAppaloosa() &&
                                        CanPerformWithKaizyle() ? 410501 : 410011,
                }
            },
            {410400, //transition to loading the song for Kaizyle
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} and {_kaizyleColoredName} are getting ready to perform together]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {410011,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'm not sure who to perform with... So I will be perfoming solo!",
                    option2DialogueID = 410012,
                }
            },
            {4100111,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Looks like nobody wanted to perform with me... So I will be perfoming solo!",
                    option2DialogueID = 410012,
                }
            },
            {410012, // SOLO ENDING
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} is getting ready for his solo performance]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {410501,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I couldn't choose who to perform with... So why don't we all perform together?",
                    option2DialogueID = 410502,
                }
            },
            {410502,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: {_sodaColoredName} I love that idea!",
                    option2DialogueID = 410503,
                }
            },
            {410503,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: That's the spirit {_sodaColoredName}, I've always like the way you think.",
                    option2DialogueID = 410504,
                }
            },
            {410504,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Playing all together would be great! Let's do it!",
                    option2DialogueID = 410505,
                }
            },
            {410505,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Together, there is no way we will lose this competition. Let's win it all!",
                    option2DialogueID = 410506,
                }
            },
            {410506, //HAREM ENDING
                new DialogueData()
                {
                    dialogueText = $"[everyone is getting ready to perform together]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            #endregion
        };

        public class DialogueData
        {
            public string option1Text = "", option2Text = "...", dialogueText = "<Color=\"red\">You forgot the text dummy</Color>";
            public int option1DialogueID = 0, option2DialogueID;
            public ScoreData option1Score = new ScoreData();
            public ScoreData option2Score = new ScoreData();
        }

        public static class DialogueFlags
        {
            #region Global
            public static bool kissedSomeone;
            #endregion

            #region Chapter 1
            public static bool cheeredTrixie;
            public static bool isCompetitive;
            public static bool welcomedAppaloosa;
            public static bool presentedFriends;
            public static bool calmedKaizyleDown;
            #endregion

            #region Chapter 2
            public static bool offeredPracticeWithTrixie;
            public static bool talkedShitAboutRock;
            public static bool offeredIdeaToBeezerly;
            public static bool pickedAppaloosa;
            public static bool pickedKaizyle;
            public static bool botheredKaizyle;
            public static bool didntPeekKaizyleRoom;
            public static bool didntPeekAppaloosaRoom;
            public static bool askedAppaloosaForHelp;
            public static bool askedKaizyleForHelp;
            public static bool annoyedTheFuckOutOfKaizyle;
            #endregion

            #region Chapter 3 part 1
            public static bool mentionedTrixiePenguinPin;
            public static bool invitedTrixieOut;
            public static bool sharedCookieWithTrixie;
            public static bool saidTheTruth;
            public static bool calledTrixieAFriend;
            public static bool awkwardMomentWithTrixie;
            public static bool toldTrixieAboutTheSmell;
            public static bool gtfoOfTheDateEarly;
            public static bool wannaMeetWithTrixieAgain;
            public static bool walkedTrixieBackHome;
            public static bool sodaAteACookie;
            public static bool trixieAteACookie;
            public static bool threwCookieInGarbage;
            public static bool kissedTrixie;
            public static bool wantsToGoToAquarium;
            #endregion

            #region Chapter 3 part 2
            public static bool askedIfFirstTime;
            public static bool orderedBurger;
            public static bool agreedWithBeezerly;
            public static bool convincedBeezerly;
            public static bool askedAboutTheFood;
            public static bool likedTheBurger;
            public static bool listenedToTheBand;
            public static bool pickedBeezerlyFavoriteSong;
            public static bool dancedWithBeezerly;
            public static bool kissedBeezerly;
            public static bool complimentedBeezerlyDancing;
            #endregion

            #region Chapter 3 part 3
            public static bool obsessAppaloosa;
            public static bool unimpressedAppaloosa;
            public static bool disinterestedAppaloosa;
            public static bool kissedAppaloosa;
            public static bool awkwardAppaloosa;
            public static bool flirtAppaloosa;
            #endregion

            #region Chapter 3 part 4
            public static bool wannaKnowAboutKaizyle;
            public static bool saidYippies;
            public static bool choosedGlissandogs;
            public static bool arguedAboutGlissandogs;
            public static bool overReactedAboutKaizyleHotdogs;
            public static bool complimentedKaizyle;
            public static bool threwIceCreamAway;
            public static bool gotIceCream;
            public static bool gotSundae;
            public static bool kissedKaizyle;
            #endregion

            #region Chapter 4
            public static bool trixiePresent;
            public static bool beezerlyPresent;
            public static bool appaloosaPresent;
            public static bool kaizylePresent;

            public static bool performedWithTrixie;
            public static bool performedWithBeezerly;
            public static bool performedWithAppaloosa;
            public static bool performedWithKaizyle;
            public static bool performedSolo;
            public static bool performedGroup;
            #endregion
        }

        public class ScoreData
        {
            public float trixieScore = 0, appaloosaScore = 0, kaizyleScore = 0, beezerlyScore = 0;

            public void AddScore(ScoreData scoreData)
            {
                trixieScore += scoreData.trixieScore;
                appaloosaScore += scoreData.appaloosaScore;
                kaizyleScore += scoreData.kaizyleScore;
                beezerlyScore += scoreData.beezerlyScore;
            }
        }

        public static void ResetCharacterPositions()
        {
            _soda.transform.position = _outLeftCharPosition;
            _trixiebell.transform.position = _outRightCharPosition;
            _beezerly.transform.position = _outRightCharPosition;
            _appaloosa.transform.position = _outRightCharPosition;
            _kaizyle.transform.position = _outRightCharPosition;
        }

        public static void RecursiveTrixieAnimation()
        {
            var rdmNum = UnityEngine.Random.Range(0, 1);
            var speed = UnityEngine.Random.Range(.2f, 1.2f);
            FlipSpriteAnimation(_trixiebell, false, speed - .1f, 1.5f / speed);
            AnimationManager.AddNewTransformPositionAnimation(_trixiebell, rdmNum <= .5f ? _rightCharPosition + new Vector3(1, 0, 0) : _farRightCharPosition, speed, GetSecondDegreeAnimationFunction(), delegate
               {
                   var speed2 = UnityEngine.Random.Range(.2f, 1.2f);
                   FlipSpriteAnimation(_trixiebell, false, speed2 - .1f, 1.5f / speed2);
                   rdmNum = UnityEngine.Random.Range(0, 1);
                   AnimationManager.AddNewTransformPositionAnimation(_trixiebell, rdmNum <= .5f ? _centerCharPosition : _rightCharPosition, speed2, GetSecondDegreeAnimationFunction(), delegate
                   {
                       if (_currentDialogueState == 100)
                           RecursiveTrixieAnimation();
                       else
                       {
                           FlipSpriteLeftAnimation(_trixiebell, false);
                           AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _rightCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                       }

                   });

               });
        }

        private static CharExpressions[] _sodaDanceSprites = { CharExpressions.SodaJam1, CharExpressions.SodaJam2, CharExpressions.SodaJam3 };
        private static CharExpressions[] _beezerlyDanceSprites = { CharExpressions.BeezJam1, CharExpressions.BeezJam2, CharExpressions.BeezJam3 };
        private static int danceSpriteIndex;
        private static int danceIncrement;
        public static void RecursiveSodaBeezerlyDanceAnimation()
        {
            if (danceIncrement != 0)
            {
                AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 0.2f, GetSecondDegreeAnimationFunction(0.0001f), delegate
                {
                    if (danceIncrement != 0)
                        ChangeCharSprite(_sodaSprite, _sodaDanceSprites[danceSpriteIndex]);
                });

                AnimationManager.AddNewTransformPositionAnimation(_beezerly, _leftCharPosition, 0.2f, GetSecondDegreeAnimationFunction(0.0001f), delegate
                {
                    if (danceIncrement != 0)
                    {
                        ChangeCharSprite(_beezerlySprite, _beezerlyDanceSprites[danceSpriteIndex]);
                        RecursiveSodaBeezerlyDanceAnimation();
                    }
                });
                danceSpriteIndex += danceIncrement;
                if (danceSpriteIndex == 2 || danceSpriteIndex == 0)
                    danceIncrement = -danceIncrement;
            }
        }

        public static void UpdateDialogueStates(int chapterID)
        {
            switch (chapterID)
            {
                case 1:
                case 2:
                    _dialogueStates = GetDialogueChapter1And2();
                    break;
                case 3:
                    _dialogueStates = GetDialogueChapter3();
                    break;
                case 4:
                    _dialogueStates = GetDialogueChapter4();
                    break;
            }
            Plugin.Instance.LogInfo("Dialogue States Update for Chapter " + chapterID);
        }

        public static void ChangeCharSprite(SpriteRenderer renderer, CharExpressions expression, Color? color = null)
        {
            if (expression != CharExpressions.None)
                renderer.sprite = TooterAssetsManager.GetSprite(ExpressionToSpritePath(expression));
            if (color != null)
                renderer.color = (Color)color;
        }

        public static string ExpressionToSpritePath(CharExpressions expression) => expression.ToString() + ".png";

        public enum CharExpressions
        {
            None,
            SodaNeutral,
            SodaNeutralTalk,
            SodaDeepSmug,
            SodaEh,
            SodaEmbarrassedLight,
            SodaShock,
            SodaStressLight,
            SodaWheezeRW,
            SodaAgree,
            SodaThinking,
            SodaPlead,
            SodaBeg,
            SodaBleh,
            SodaEat,
            SodaHoldCookie,
            SodaMunch,
            SodaWow,
            SodaInLove,
            SodaCall,
            SodaHype,
            SodaFightMe,
            SodaBreaking,
            SodaJam1,
            SodaJam2,
            SodaJam3,
            SodaBone,

            TrixieNeutral,
            TrixieNeutralTalk,
            TrixieAnxious,
            TrixieCompliment1,
            TrixieCompliment2,
            TrixieCompliment3,
            TrixiePanic,
            TrixiePleased,
            TrixieSadge,
            TrixieBag,
            TrixieAgree,
            TrixieAmaze,
            TrixieEat,
            TrixieHoldCookie,
            TrixieSurprise,
            TrixieInLove,

            AppaloosaNeutral,
            AppaloosaNeutralTalk,
            AppaloosaCall,
            AppaloosaLeanAway,
            AppaloosaLOL,
            AppaloosaInLove,
            AppaloosaBlush,
            AppaloosaWow,
            AppaloosaAgree,
            AppaloosaDisappointed,
            AppaloosaBone,

            BeezerlyNeutral,
            BeezerlyNeutralTalk,
            BeezerlyAggro,
            BeezerlyMock,
            BeezerlyBump,
            BeezerlyImpressed,
            BeezerlyThinking,
            BeezerlyUh,
            BeezerlyChallenge,
            BeezerlyInLove,
            BeezerlyHype,
            BeezerlyPassion,
            BeezerlyWhat,
            BeezerlyPoint,
            BeezJam1,
            BeezJam2,
            BeezJam3,

            KaizyleNeutral,
            KaizyleNeutralTalk,
            KaizyleDispleased,
            KaizyleWTF,
            KaizyleConcern,
            KaizyleBrag,
            KaizylePissed,
            KaizyleUm,
            KaizyleFightMe,
            KaizyleCat,
            KaizyleNom,
            KaizyleFine,
            KaizyleShy,
            KaizyleEnamored,
            KaizyleFlatteredLookUp,
            KaizyleProud,
            KaizyleShrug,
            KaizyleFlattered,
            KaizyleLove,
            KaizyleSmirk,

            HornLordNeutral,
            HornLordTalk,
            HornLordYeah,
        }
    }
}
