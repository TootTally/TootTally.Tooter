using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TootTally.Graphics;
using TootTally.Graphics.Animation;
using TootTally.Utils;
using TootTally.Utils.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
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
        private static GameObject _fadeOutPanel;
        private static PopUpNotif _txtBox;
        private static int _currentDialogueState;
        private static readonly Vector2 _btn2PositionRight = new Vector2(305, -180);
        private static readonly Vector2 _btn2PositionCenter = new Vector2(65, -190);

        private static readonly Vector3 _leftCharPosition = new Vector3(-8f, -6.5f, 10);
        private static readonly Vector3 _leftCenterCharPosition = new Vector3(-5f, -6.5f, 10);
        private static readonly Vector3 _centerCharPosition = new Vector3(-1.5f, -6.5f, 10);
        private static readonly Vector3 _rightCenterCharPosition = new Vector3(1f, -6.5f, 10);
        private static readonly Vector3 _rightCharPosition = new Vector3(4f, -6.5f, 10);
        private static readonly Vector3 _farRightCharPosition = new Vector3(6.8f, -6.5f, 10);
        private static readonly Vector3 _outLeftCharPosition = new Vector3(-15, -6.5f, 10);
        private static readonly Vector3 _outRightCharPosition = new Vector3(15, -6.5f, 10);
        private static readonly string _sodaColoredName = "<color='#FFFF21'>Soda</color>";
        private static readonly string _trixieColoredName = "<color='#FFAAAA'>Trixiebell</color>";
        private static readonly string _appaloosaColoredName = "<color='#FF0000'>Appaloosa</color>";
        private static readonly string _beezerlyColoredName = "<color='#f0f0c2'>Beezerly</color>";
        private static readonly string _kaizyleColoredName = "<color='#A020F0'>Kaizyle</color>";

        public static void OnModuleLoad()
        {
            _tooterButtonLoaded = false;
        }


        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.addWord))]
        [HarmonyPostfix]
        public static void OnDemonDialogueAddWordPostFix(object[] __args)
        {
            _txtBox.UpdateText(_txtBox.GetText + __args[0] + " "); //base game does it like that xd...
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
            __instance.btn1obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-65, -190);
            __instance.btn2obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(65, -180);
            __instance.txtbox.gameObject.SetActive(false);
            SetCharacterPrefab();
            _soda = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Soda", _outLeftCharPosition, TooterAssetsManager.GetSprite("SodaSmiling.png"));
            _sodaSprite = _soda.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _appaloosa = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Appaloosa", _outRightCharPosition, TooterAssetsManager.GetSprite("AppaloosaNeutral.png"));
            _appaloosaSprite = _appaloosa.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _beezerly = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Beezerly", _outRightCharPosition, TooterAssetsManager.GetSprite("BeezerlyNeutral.png"));
            _beezerlySprite = _beezerly.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _kaizyle = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Kaizyle", _outRightCharPosition, TooterAssetsManager.GetSprite("KaizyleNeutral.png"));
            _kaizyleSprite = _kaizyle.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _trixiebell = CreateCharacterFromPrefab(GameObject.Find("CAM_demonpuppet").transform, "Trixiebell", _outRightCharPosition, TooterAssetsManager.GetSprite("TrixieNeutral.png"));
            _trixiebellSprite = _trixiebell.transform.Find("demon-def-body").GetComponent<SpriteRenderer>();

            _txtBox = GameObjectFactory.CreateNotif(GameObject.Find("CAM_middletier/DemonCanvas/Panel").transform, "NovelTextBox", "", GameTheme.themeColors.notification.defaultText);
            RectTransform txtBoxRectTransform = _txtBox.GetComponent<RectTransform>();
            _txtBox.transform.SetSiblingIndex(1);
            txtBoxRectTransform.anchoredPosition = new Vector2(0, -300);
            txtBoxRectTransform.sizeDelta = new Vector2(1500, 250);
            txtBoxRectTransform.localScale = Vector2.one / 2f;
            _txtBox.Initialize(float.MaxValue, new Vector2(0, -150));
            _txtBox.SetTextSize(32); //SetTextSize has to be called after init
            __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
            __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().color = new Color(.6f, .6f, .6f);
            __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().preserveAspect = true;
            __instance.csc.demonbg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            __instance.csc.demonbg.GetComponent<RectTransform>().localScale = Vector2.one * 8f;
            GameObject.DestroyImmediate(__instance.csc.demonbg.transform.Find("Image (1)").gameObject);

            Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("Chapter1-2Music.mp3", clip =>
            {
                __instance.csc.bgmus2.clip = clip;
                __instance.csc.bgmus2.volume = .25f;
                __instance.csc.bgmus2.Play();
            }));
            Plugin.Instance.StartCoroutine(TryLoadingAudioClipLocal("Chapter3Music.mp3", clip =>
            {
                __instance.csc.bgmus1.clip = clip;
                __instance.csc.bgmus1.volume = 0f;
            }));
            _scoreData = new ScoreData();
        }

        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.clickbtn1))]
        [HarmonyPrefix]
        public static bool OverwriteClickBtn1(DemonDialogue __instance)
        {
            if (__instance.readytoclick)
            {
                __instance.readytoclick = false;
                __instance.resetClickTimer();
                //__instance.csc.sfx_buttons[1].Play();
                _scoreData.AddScore(DialogueStates[_currentDialogueState].option1Score);
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
                __instance.resetClickTimer();
                //__instance.csc.sfx_buttons[1].Play();
                _scoreData.AddScore(DialogueStates[_currentDialogueState].option2Score);
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
                __instance.Invoke("startDemonDiag", 0f);
                __instance.killDemonCards();
            }
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
            GameObject tooterText = GameObject.Instantiate(__instance.paneltxts[(int)HomeScreenButtonIndexes.Collect], mainMenu.transform);
            tooterButton.name = "TOOTERContainer";
            tooterHitbox.name = "TOOTERButton";
            tooterText.name = "TOOTERText";
            OverwriteGameObjectSpriteAndColor(tooterButton.transform.Find("FG").gameObject, "TooterButton.png", Color.white);
            OverwriteGameObjectSpriteAndColor(tooterText, "TooterText.png", Color.white);
            tooterButton.transform.SetSiblingIndex(0);
            RectTransform tooterTextRectTransform = tooterText.GetComponent<RectTransform>();
            tooterTextRectTransform.anchoredPosition = new Vector2(100, 100);
            tooterTextRectTransform.sizeDelta = new Vector2(456, 89);

            _tooterButtonOutlineRectTransform = tooterButton.transform.Find("outline").GetComponent<RectTransform>();
            _tooterButtonClicked = false;
            tooterHitbox.GetComponent<Button>().onClick.AddListener(() =>
            {
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

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.doFastScreenShake))]
        [HarmonyPrefix]
        public static bool GetRidOfThatScreenShakePls(HomeController __instance) => false; //THANKS GOD

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Update))]
        [HarmonyPostfix]
        public static void AnimateTooterButton(HomeController __instance)
        {
            if (_tooterButtonLoaded)
                _tooterButtonOutlineRectTransform.transform.parent.transform.Find("FG/texholder").GetComponent<CanvasGroup>().alpha = (_tooterButtonOutlineRectTransform.localScale.y - 0.4f) / 1.5f;
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
            __instance.dtxt(DialogueStates[_currentDialogueState].dialogueText);
            __instance.btns(DialogueStates[_currentDialogueState].option1Text, DialogueStates[_currentDialogueState].option2Text, DialogueStates[_currentDialogueState].option1DialogueID, DialogueStates[_currentDialogueState].option2DialogueID);

            __instance.btn2obj.GetComponent<RectTransform>().anchoredPosition = DialogueStates[_currentDialogueState].option1Text != "" ? _btn2PositionCenter : _btn2PositionRight;

            Plugin.Instance.LogInfo("Event #" + _currentDialogueState);
            //Add dialogue specific events here
            switch (_currentDialogueState)
            {
                case 1:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 2:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaSmiling, Color.white);
                    FlipSpriteAnimation(_soda, true);
                    break;
                case 3:
                    FlipSpriteAnimation(_soda, true, 3f);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 4:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 6:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    DialogueFlags.cheeredTrixie = true;
                    break;
                case 8:
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _rightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 12:
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _leftCenterCharPosition, 0.9f, GetSecondDegreeAnimationFunction(), delegate
                    {
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    });
                    break;
                case 13:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    DialogueFlags.isCompetitive = true;
                    break;
                case 14:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 16:
                    FlipSpriteAnimation(_trixiebell, true);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _rightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 17:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    DialogueFlags.welcomedAppaloosa = true;
                    break;
                case 18:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    break;
                case 20:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    DialogueFlags.presentedFriends = true;
                    break;
                case 22:
                case 23:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    break;
                case 24:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    break;
                case 26:
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCenterCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaSmiling, Color.white);
                    break;
                case 27:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    break;
                case 30:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    DialogueFlags.calmedKaizyleDown = true;
                    break;
                case 35:
                    FlipSpriteAnimation(_soda, true);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 2.65f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f), delegate { FadeOutScene(__instance, 36); });
                    FlipSpriteAnimation(_trixiebell, true, .9f);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outLeftCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_beezerly, true, .8f);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_appaloosa, true, 1.2f);
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _outRightCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    FlipSpriteAnimation(_kaizyle, true);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _outRightCharPosition, 4f, new EasingHelper.SecondOrderDynamics(.25f, 1f, 0f));
                    break;
                case 36:
                    FlipSpriteAnimation(_trixiebell, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _rightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 37:
                    FlipSpriteAnimation(_soda, false);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    FlipSpriteAnimation(_trixiebell, true);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 39:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    DialogueFlags.offeredPracticeWithTrixie = true;
                    break;
                case 40:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 42:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 43:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaSmiling, Color.white);
                    FlipSpriteAnimation(_trixiebell, false);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    FlipSpriteAnimation(_beezerly, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 46:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                    DialogueFlags.talkedShitAboutJazz = true;
                    break;
                case 48:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 49:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    DialogueFlags.offeredIdeaToBeezerly = true;
                    break;
                case 54:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaSmiling, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 55:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.pickedAppaloosa = DialogueFlags.pickedKaizyle = false;
                    break;
                case 56:
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _leftCenterCharPosition + new Vector3(.8f, 0, 0), 1f, GetSecondDegreeAnimationFunction(.8f), delegate
                      {
                          ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
                          FlipSpriteAnimation(_beezerly, false, 5f);
                          AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction(), delegate
                          {
                              ChangeCharSprite(_sodaSprite, CharExpressions.SodaSmiling, Color.white);
                          });
                      });
                    break;
                case 57:
                    _appaloosa.transform.position = _outLeftCharPosition; //tp her to the left xd YEET
                    DialogueFlags.pickedAppaloosa = true;
                    break;
                case 58:
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    FlipSpriteAnimation(_soda, true);
                    break;
                case 59:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 61:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    DialogueFlags.askedAppaloosaForHelp = true;
                    break;
                case 66:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 67:
                    DialogueFlags.pickedKaizyle = true;
                    break;
                case 68:
                    FlipSpriteAnimation(_kaizyle, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    break;
                case 70:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    DialogueFlags.askedKaizyleForHelp = true;
                    break;
                case 72:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition + new Vector3(1.2f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition + new Vector3(.4f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    break;
                case 74:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition + new Vector3(2.6f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition + new Vector3(.8f, 0, 0), 1f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.annoyedTheFuckOutOfKaizyle = true;
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWheezeRW, Color.white);
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
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 78:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    break;
                case 79:
                case 80:
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    FlipSpriteAnimation(_soda, false);
                    break;
                case 81:
                    FadeOutScene(__instance, 82);
                    break;
                case 82:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    break;





                case 10:
                case 21:
                case 31:
                case 44:
                case 51:
                case 60:
                case 63:
                case 64:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNormalTalk, Color.white);
                    break;
                case 5:
                case 7:
                case 11:
                case 15:
                case 19:
                case 32:
                case 38:
                case 45:
                case 50:
                case 52:
                case 62:
                case 65:
                case 69:
                case 71:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaSmiling, Color.white);
                    break;
            }

            return false;
        }

        public static void FadeOutScene(DemonDialogue __instance, int nextDialogueID)
        {



            __instance.csc.fadeoutpanel.transform.localScale = new Vector3(2f, 0.001f, 1f);
            __instance.csc.fadeoutpanel.SetActive(true);
            __instance.csc.fadeMus(0, false);
            __instance.csc.fadeMus(1, false);
            AnimationManager.AddNewTransformScaleAnimation(__instance.csc.fadeoutpanel, new Vector3(2f, 2f, 1f), 2f, GetSecondDegreeAnimationFunction(), delegate
            {
                switch (nextDialogueID)
                {
                    //end chapter 1
                    case 36:
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("ClassroomEvening.png");
                        _txtBox.UpdateText("");
                        __instance.csc.fadeMus(1, true);
                        LogChapter1States();
                        break;

                    //end chapter 2
                    case 82:
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        _txtBox.UpdateText("");
                        __instance.csc.bgmus1.time = 0;
                        __instance.csc.fadeMus(0, true);
                        __instance.csc.bgmus1.Play();
                        _soda.transform.position = _outRightCharPosition;
                        _trixiebell.transform.position = _outLeftCharPosition;
                        DialogueStates = GetDialogueChapter3();
                        LogChapter2States();
                        break;
                }
                LogScores();
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
            Plugin.Instance.LogInfo("   talkedShitAboutJazz: " + DialogueFlags.talkedShitAboutJazz);
            Plugin.Instance.LogInfo("   offeredIdeaToBeezerly: " + DialogueFlags.offeredIdeaToBeezerly);
            Plugin.Instance.LogInfo("   pickedAppaloosa: " + DialogueFlags.pickedAppaloosa);
            Plugin.Instance.LogInfo("   pickedKaizyle: " + DialogueFlags.pickedKaizyle);
            Plugin.Instance.LogInfo("   askedAppaloosaForHelp: " + DialogueFlags.askedAppaloosaForHelp);
            Plugin.Instance.LogInfo("   offeredIdeaToBeezerly: " + DialogueFlags.offeredIdeaToBeezerly);
            Plugin.Instance.LogInfo("   askedKaizyleForHelp: " + DialogueFlags.askedKaizyleForHelp);
            Plugin.Instance.LogInfo("   annoyedTheFuckOutOfKaizyle: " + DialogueFlags.annoyedTheFuckOutOfKaizyle);
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

        public static Dictionary<int, DialogueData> DialogueStates = new Dictionary<int, DialogueData>
        {
            #region CHAPTER 1 INTRO
            {0,
                new DialogueData()
                {
                    dialogueText = $"???: I can't wait for the music competition this year.",
                    option2DialogueID = 1
                }
            },
            {1,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I've been practicing so hard, and I really want to win.",
                    option2DialogueID = 2
                }
            },
            {2,
                new DialogueData()
                {
                    dialogueText = $"[Someone else enters the room]",
                    option2DialogueID = 3,

                }
            },
            {3,
                new DialogueData()
                {
                    dialogueText = $"???: Oh, sorry. I didn't mean to interrupt. I was just looking for my music sheet.",
                    option2DialogueID = 4
                }
            },
            {4,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh, hey {_trixieColoredName}. It's okay, you're not interrupting anything.",
                    option2DialogueID = 5
                }
            },
            {5,
                new DialogueData()
                {
                    dialogueText =  $"{_trixieColoredName}: Thanks. I'm really nervous about the competition this year. I don't know if I can do it.",
                    option1Text = "Cheer",
                    option1DialogueID = 6,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 3f
                    },
                    option2Text = "Ignore",
                    option2DialogueID = 7,
                }
            },
            {6,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Don't worry, {_trixieColoredName}. You're a great player. I'm sure you'll do great.",
                    option2DialogueID = 7
                }
            },
            {7,
                new DialogueData()
                {
                    dialogueText = $"[As they continue to chat, the door opens again and another girl walks in]",
                    option2DialogueID = 8
                }
            },
            {8,
                new DialogueData()
                {
                    dialogueText = $"???: Hey there, music nerds. What's going on?",
                    option2DialogueID = 10
                }
            },
            {10,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_beezerlyColoredName}. Just getting ready for the competition.",
                    option2DialogueID = 11
                }
            },
            {11,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Yeah, yeah, the competition. Whatever. I'm just here to jam and have some fun.",
                    option2DialogueID = 12
                }
            },
            {12,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName} [Whispering]: I don't think she takes music very seriously.",
                    option1Text = "I'm competitive",
                    option1DialogueID = 13,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -1,
                        beezerlyScore = 1,
                    },
                    option2Text = "I'm casual",
                    option2DialogueID = 14,
                    option2Score = new ScoreData()
                    {
                        trixieScore = 3,
                        beezerlyScore = 1,
                    }
                }
            },
            {13,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, everyone has their own way of enjoying music. Maybe we can show her the fun in the competition too.",
                    option2DialogueID = 15
                }
            },
            {14,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, everyone has their own way of enjoying music. Having fun should be the top most reason to play music.",
                    option2DialogueID = 15
                }
            },
            {15,
                new DialogueData()
                {
                    dialogueText = $"[As they continue to chat, the door opens again as another girl enters the room, carrying a sleek, professional-looking trombone]",
                    option2DialogueID = 16
                }
            },
            {16,
                new DialogueData()
                {
                    dialogueText = $"???: Hey, everyone. Is this where the cool trombone players hang out?",
                    option1Text = "Welcome in!",
                    option1DialogueID = 17,
                    option1Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                    },
                    option2Text = "Guess so...",
                    option2DialogueID = 18
                }
            },
            {17,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Absolutely! What have you been up to?",
                    option2DialogueID = 19
                }
            },
            {18,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I guess so. What brings you here?",
                    option2DialogueID = 19
                }
            },
            {19,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: I heard there were some talented players in this room, and I wanted to see for myself. I'm {_appaloosaColoredName}, by the way.",
                    option1Text = "Show friends",
                    option1DialogueID = 20,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 1,
                        beezerlyScore = 1,
                        appaloosaScore = 2,
                    },
                    option2Text = "Im Soda",
                    option2DialogueID = 21,
                    option2Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                    }
                }
            },
            {20,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Nice to meet you, {_appaloosaColoredName}. I'm {_sodaColoredName}, and these are my bandmates {_trixieColoredName} and {_beezerlyColoredName}.",
                    option2DialogueID = 22
                }
            },
            {21,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Nice to meet you, {_appaloosaColoredName}. I'm {_sodaColoredName}!",
                    option2DialogueID = 23
                }
            },
            {22,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Cool names. I like your style, {_sodaColoredName}. Want to jam sometime?",
                    option2DialogueID = 24
                }
            },
            {23,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: I like your style, {_sodaColoredName}. Want to jam sometime?",
                    option2DialogueID = 24
                }
            },
            {24,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Definitely. That would be awesome.",
                    option2DialogueID = 25
                }
            },
            {25,
                new DialogueData()
                {
                    dialogueText = $"[The door opens again and another girl enters the room, carrying a fancy, gold-plated trombone]",
                    option2DialogueID = 26
                }
            },
            {26,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Good afternoon, everyone. I'm {_kaizyleColoredName}, and I'm here to rehearse for the competition.",
                    option2DialogueID = 27
                }
            },
            {27,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Oh great, another snobby classical player.",
                    option2DialogueID = 29
                }
            },
            {29,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Excuse me? I beg your pardon, but I come from a long line of respected musicians. I take my craft very seriously.",
                    option1Text = "Calm down",
                    option1DialogueID = 30,
                    option1Score = new ScoreData()
                    {
                        kaizyleScore = 1,
                    },
                    option2Text = "Welcome",
                    option2DialogueID = 31,
                    option2Score = new ScoreData()
                    {
                        kaizyleScore = -1,
                        beezerlyScore = 1,
                    },
                }
            },
            {30,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's nice to meet you, {_kaizyleColoredName}. We're all here for the same reason, right? To make beautiful music?",
                    option2DialogueID = 32
                }
            },
            {31,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's nice to meet you, {_kaizyleColoredName}. Lets play some music!",
                    option2DialogueID = 33
                }
            },
            {32,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yes, I suppose you're right. Let's get started then.",
                    option2DialogueID = 34
                }
            },
            {33,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yes, Let's get started!",
                    option2DialogueID = 34
                }
            },
            {34,
                new DialogueData()
                {
                    dialogueText = $"[The characters take their seats and start to practice their instruments]",
                    option2DialogueID = 35
                }
            },
            {35,
                new DialogueData()
                {
                    dialogueText = $"[END OF CHAPTER 1]",
                    option2Text = "",
                    option2DialogueID = 0
                }
            },
            #endregion

            #region CHAPTER 2 
            {36,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 2: GETTING TO KNOW THE GIRLS",
                    option2DialogueID = 37
                }
            },

            #region Trixie Interaction
            {37,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_trixieColoredName}, how are you feeling about the competition?",
                    option2DialogueID = 38
                }
            },
            {38,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Oh, I'm so nervous. I've been practicing so hard, but I'm still afraid I'll mess up on stage.",
                    option1Text = "Practice together",
                    option1DialogueID = 39,
                    option2Text = "Stop stressing out",
                    option2DialogueID = 40
                }
            },
            {39,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I know how you feel. Maybe we can practice together and help each other out?",
                    option2DialogueID = 41
                }
            },
            {40,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Everything will be fine, there is no reason to be stressed.",
                    option2DialogueID = 42
                }
            },
            {41,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: That would be amazing! Thank you, Soda. I'll see you later!",
                    option2DialogueID = 43
                }
            },
            {42,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thanks I guess... I have to go back to practice. Bye!",
                    option2DialogueID = 43
                }
            },
            {43,
                new DialogueData()
                {
                    dialogueText = $"[{_trixieColoredName} goes back to practicing, {_beezerlyColoredName} walks into the room]",
                    option2DialogueID = 44
                }
            },
            #endregion

            #region Beezerly Interaction
            {44,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_beezerlyColoredName}! What kind of music do you like to play on the trombone?",
                    option2DialogueID = 45
                }
            },
            {45,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: I like to mix it up, you know? Sometimes I'll play something jazzy or funky, and other times I'll play something classical. I don't like to be boxed in.",
                    option1DialogueID = 46,
                    option1Text = "I hate jazz",
                    option2DialogueID = 48,
                    option2Text = "I love jazz"
                }
            },
            {46,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Ewww jazz? I prefer listening to real music like Rock or Metal",
                    option2DialogueID = 47,
                }
            },
            {47,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Really? There is no such thing as \"real music\". Anyway, I have to go practice. Bye!",
                    option2DialogueID = 54,
                }
            },
            {48,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Jazz is really cool! I listen to Jazz music all the time.",
                    option1Text = "Offer Idea",
                    option1DialogueID = 49,
                    option2Text = "Let Her Practice",
                    option2DialogueID = 51,
                }
            },
            {49,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Maybe we can come up with something tootally unique for the competition?",
                    option2DialogueID = 50,
                }
            },
            {50,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName} : Sure! I like the way you think, {_sodaColoredName}.",
                    option2DialogueID = 53,
                }
            },
            {51,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I will let you go practice, it was nice talking with you!",
                    option2DialogueID = 52,
                }
            },
            {52,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Thanks {_sodaColoredName}! I'll catch you later.",
                    option2DialogueID = 54,
                }
            },
            {53,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Anyway, I have to go back to practice. I'll catch you later {_sodaColoredName}!",
                    option2DialogueID = 56,
                }
            },
            {54,
                new DialogueData()
                {
                    dialogueText = $"[{_beezerlyColoredName} leaves the room]",
                    option2DialogueID = 55,
                }
            },
            {55,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName} to himself: What should I do now?",
                    option1Text = "Talk to Appaloosa",
                    option1DialogueID = 57,
                    option2Text = "Talk to Kaizyle",
                    option2DialogueID = 67,
                }
            },
            {56,
                new DialogueData()
                {
                    dialogueText = $"[{_beezerlyColoredName} lightly fist bump {_sodaColoredName}'s shoulder and then leaves the room with a smirky smile on her face]",
                    option2DialogueID = 55,
                }
            },
            #endregion

            #region Appaloosa Interaction
            {57,
                new DialogueData()
                {
                    dialogueText = $"[A beautiful and calming melody can be heard from a nearby room. Peek in the room?]",
                    option1Text = "Yes",
                    option1DialogueID = 58,
                    option2Text = "No",
                    option2DialogueID = 55,
                }
            },
            {58,
                new DialogueData()
                {
                    dialogueText = $"[You peek in the other room and see {_appaloosaColoredName} practicing her improvisation]",
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
                    dialogueText = $"{_appaloosaColoredName}: It's all about feeling the music, you know? You gotta let go of your inhibitions and just let the music flow through you.",
                    option1Text = "Teach me!",
                    option1DialogueID = 61,
                    option2Text = "Let her practice",
                    option2DialogueID = 64,
                }
            },
            {61,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That sounds amazing. Can you teach me how to improvise like you do?",
                    option2DialogueID = 62,
                }
            },
            {62,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Sure thing, {_sodaColoredName}. Let's jam after school!",
                    option2DialogueID = 63,
                }
            },
            {63,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Awesome! I will see you after school.",
                    option2DialogueID = 66,
                }
            },
            {64,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I will let you go practice, it was nice talking with you!",
                    option2DialogueID = 65,
                }
            },
            {65,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Thanks {_sodaColoredName}!",
                    option2DialogueID = 66,
                }
            },
            {66,
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} leaves the room]",
                    option2DialogueID = 81,
                }
            },
            #endregion

            #region Kaizyle Interaction
            {67,
                new DialogueData()
                {
                    dialogueText = $"[A fast and complex melody can be heard from another room. Peek in the other room?]",
                    option1Text = "Yes",
                    option1DialogueID = 68,
                    option2Text = "No",
                    option2DialogueID = 55,
                }
            },
            {68,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: {_kaizyleColoredName}, I'm really impressed by your technical skill on the trombone.",
                    option2DialogueID = 69,
                }
            },
            {69,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Thank you, {_sodaColoredName}. I come from a long line of musicians, so I take my training very seriously.",
                    option1Text = "Teach me!",
                    option1DialogueID = 70,
                    option2Text = "Let her practice",
                    option2DialogueID = 77,
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
                    dialogueText = $"{_kaizyleColoredName}: I would like to help but I really need to finish my practice. We can talk later.",
                    option1Text = "Beg for help",
                    option1DialogueID = 72,
                    option2Text = "Let her practice",
                    option2DialogueID = 77,
                }
            },
            {72,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: PLEASE {_kaizyleColoredName} I really need to improve my technique and I'd love to become as good as you in the future.",
                    option2DialogueID = 73,
                }
            },
            {73,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I told you I need to practice. I will be able to help you later so please let me practice now.",
                    option1Text = "Keep begging",
                    option1DialogueID = 74,
                    option2Text = "Let her practice",
                    option2DialogueID = 78,
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
                    dialogueText = $"[{_kaizyleColoredName} left the room]",
                    option2DialogueID = 81,
                }
            },
            {77,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName} I will let you practice now. I'll see you later!",
                    option2DialogueID = 79,
                }
            },
            {78,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Pardon me. I will let you practice now. Bye!",
                    option2DialogueID = 80,
                }
            },
            {79,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName} to herself: How dare he disturb my practice time... that was annoying.",
                    option2DialogueID = 81,
                }
            },
            {80,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName} to herself: Well, what an annoying little brat.",
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
            {82,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 3: DATING TIME",
                    option2DialogueID = 83,
                }
            },
            {83,
                new DialogueData()
                {
                    dialogueText = $"Soda: Hey Trixiebell, how's it going?",
                    option2DialogueID = 84,
                }
            },
            {84,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.cheeredTrixie ?
                    $"Trixiebell: Oh, hi Soda. I'm great! Just practicing for the big competition." :
                    $"Trixiebell: Oh, hi Soda. I'm doing okay, I guess. Just practicing for the big competition.",
                    option2DialogueID = DialogueFlags.cheeredTrixie ? 85 : 86,
                }
            },
            {85,
                new DialogueData()
                {
                    dialogueText = $"Trixiebell: Thanks for cheering me up earlier. It really helped me release some stress and I appreciate it.",
                    option2DialogueID = 86,
                }
            },
            {86,
                new DialogueData()
                {
                    dialogueText = $"Soda: Yeah, I heard about that.{(DialogueFlags.cheeredTrixie ? " You're really talented on the trombone." : "")}",
                    option2DialogueID = 87,
                }
            },
            {87,
                new DialogueData()
                {
                    dialogueText = $"Trixiebell: {(DialogueFlags.cheeredTrixie ? "Thank you. But " : "")}I'm just so nervous about performing in front of a big audience.",
                    option2DialogueID = 88,
                }
            },
        };

        public class DialogueData
        {
            public string option1Text = "", option2Text = "...", dialogueText;
            public int option1DialogueID = 0, option2DialogueID;
            public ScoreData option1Score = new ScoreData();
            public ScoreData option2Score = new ScoreData();
        }

        public static class DialogueFlags
        {
            #region Chapter 1
            public static bool cheeredTrixie;
            public static bool isCompetitive;
            public static bool welcomedAppaloosa;
            public static bool presentedFriends;
            public static bool calmedKaizyleDown;
            #endregion

            #region Chapter 2
            public static bool offeredPracticeWithTrixie;
            public static bool talkedShitAboutJazz;
            public static bool offeredIdeaToBeezerly;
            public static bool pickedAppaloosa;
            public static bool pickedKaizyle;
            public static bool askedAppaloosaForHelp;
            public static bool askedKaizyleForHelp;
            public static bool annoyedTheFuckOutOfKaizyle;
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

        public static void ChangeCharSprite(SpriteRenderer renderer, CharExpressions expression, Color color)
        {
            if (expression != CharExpressions.None)
                renderer.sprite = TooterAssetsManager.GetSprite(ExpressionToSpritePath(expression));
            renderer.color = color;
        }
        public static string ExpressionToSpritePath(CharExpressions expression) => expression.ToString() + ".png";

        public enum CharExpressions
        {
            None,
            SodaDeepSmug,
            SodaEh,
            SodaEmbarrassedLight,
            SodaNormalTalk,
            SodaShock,
            SodaSmiling,
            SodaStressLight,
            SodaWheezeRW,
            SodaAgree,
            SodaThinking,
        }
    }
}
