using HarmonyLib;
using System;
using System.Collections;
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
        private static List<Coroutine> _textCoroutines = new List<Coroutine>();

        public static void OnModuleLoad()
        {
            _tooterButtonLoaded = false;
            _dialogueStates = GetDialogueChapter1And2();
        }


        [HarmonyPatch(typeof(DemonDialogue), nameof(DemonDialogue.addWord))]
        [HarmonyPostfix]
        public static void OnDemonDialogueAddWordPostFix(object[] __args)
        {
            //_txtBox.UpdateText(_txtBox.GetText + __args[0] + " "); //base game does it like that xd...
        }

        private static IEnumerator addWord(DemonDialogue __instance, string word, float delayTime)
        {
            float seconds = delayTime * 0.035f;
            yield return new WaitForSeconds(seconds);
            _txtBox.UpdateText(_txtBox.GetText + word + " ");
            _textCoroutines.RemoveAt(0);
            if (_textCoroutines.Count <= 0)
            {
                AnimationManager.AddNewScaleAnimation(__instance.btn1obj, new Vector3(1,1,0), .45f, new EasingHelper.SecondOrderDynamics(4.25f, .8f, 1.2f));
                AnimationManager.AddNewScaleAnimation(__instance.btn2obj, new Vector3(1,1,0), .45f, new EasingHelper.SecondOrderDynamics(4.25f, .8f, 1.2f), delegate { __instance.readytoclick = true; });
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
            AnimationManager.AddNewTransformScaleAnimation(character, new Vector3(-Math.Abs(scaleX), scaleY, 10f), 1.8f, new EasingHelper.SecondOrderDynamics(1.25f * speedMult, 1f, 0f), delegate { character.transform.localScale = new Vector2(Mathf.Sign(-Math.Abs(scaleX)) * 1.6f, 1.6f); });
            if (fixPosition)
                AnimationManager.AddNewTransformPositionAnimation(character, character.transform.position + new Vector3(Mathf.Sign(Math.Abs(scaleX)) * 1.1f, 0, 0), 1.8f, GetSecondDegreeAnimationFunction(speedMult));

        }

        public static void FlipSpriteLeftAnimation(GameObject character, bool fixPosition, float speedMult = 1f)
        {
            var scaleX = character.transform.localScale.x;
            var scaleY = character.transform.localScale.y;
            AnimationManager.AddNewTransformScaleAnimation(character, new Vector3(Math.Abs(scaleX), scaleY, 10f), 1.8f, new EasingHelper.SecondOrderDynamics(1.25f * speedMult, 1f, 0f), delegate { character.transform.localScale = new Vector2(Mathf.Sign(Math.Abs(scaleX)) * 1.6f, 1.6f); });
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
                __instance.setDescText(0);
                __instance.desctext.text = "Don't hate on rock and take your time!";
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
            __instance.dtxt(_dialogueStates[_currentDialogueState].dialogueText);
            __instance.btns(_dialogueStates[_currentDialogueState].option1Text, _dialogueStates[_currentDialogueState].option2Text, _dialogueStates[_currentDialogueState].option1DialogueID, _dialogueStates[_currentDialogueState].option2DialogueID);

            __instance.btn2obj.GetComponent<RectTransform>().anchoredPosition = _dialogueStates[_currentDialogueState].option1Text != "" ? _btn2PositionCenter : _btn2PositionRight;

            Plugin.Instance.LogInfo("Event #" + _currentDialogueState);
            //Add dialogue specific events here
            switch (_currentDialogueState)
            {
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
                case 110204:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _leftCenterCharPosition, 0.9f, GetSecondDegreeAnimationFunction(), delegate
                    {
                        ChangeCharSprite(_sodaSprite, CharExpressions.SodaStressLight, Color.white);
                    });
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
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
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
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    break;
                case 110802:
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
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
                case 110900:
                case 111000:
                    FlipSpriteAnimation(_kaizyle, true, 0.5f);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    DialogueFlags.calmedKaizyleDown = _currentDialogueState == 110900;
                    UpdateDialogueStates(1);
                    break;
                case 110901:
                case 111001:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutralTalk, Color.white);
                    break;
                case 111002:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    break;
                case 111003:
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
                case 210000:
                    FlipSpriteAnimation(_trixiebell, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _rightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 37:
                    FlipSpriteAnimation(_soda, false);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    FlipSpriteAnimation(_trixiebell, true);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 38:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    break;
                case 39:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    DialogueFlags.offeredPracticeWithTrixie = true;
                    UpdateDialogueStates(2);
                    break;
                case 40:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 41:
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 42:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEh, Color.white);
                    ChangeCharSprite(_trixiebellSprite, DialogueFlags.offeredPracticeWithTrixie ? CharExpressions.TrixieNeutral : CharExpressions.TrixieAnxious, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 43:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieSadge, Color.white);
                    FlipSpriteAnimation(_trixiebell, false);
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction(0.6f), delegate
                    {
                        FlipSpriteAnimation(_beezerly, false, 10f);
                        AnimationManager.AddNewTransformPositionAnimation(_beezerly, _farRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    });


                    break;
                case 44:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    break;
                case 45:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    break;
                case 46:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaBleh, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    DialogueFlags.talkedShitAboutRock = true;
                    UpdateDialogueStates(2);
                    break;
                case 47:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyImpressed, Color.white);
                    break;
                case 48:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    break;
                case 49:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyThinking, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    DialogueFlags.offeredIdeaToBeezerly = true;
                    UpdateDialogueStates(2);
                    break;
                case 50:
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyMock, Color.white); //beezerlyYesss emote
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 51:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutral, Color.white);
                    break;
                case 52:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyNeutralTalk, Color.white);
                    break;
                case 53:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_beezerlySprite, CharExpressions.BeezerlyAggro, Color.white);
                    break;
                case 54:
                    FlipSpriteAnimation(_beezerly, false);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _outRightCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
                    break;
                case 55:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaThinking, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _centerCharPosition, 1.5f, GetSecondDegreeAnimationFunction());
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
                    AnimationManager.AddNewTransformPositionAnimation(_appaloosa, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaWow, Color.white);
                    FlipSpriteAnimation(_soda, true);
                    break;
                case 59:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    ChangeCharSprite(_appaloosaSprite, CharExpressions.AppaloosaNeutral, Color.white);
                    break;
                case 60:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
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
                    FlipSpriteAnimation(_soda, false, 0.75f);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 67:
                    DialogueFlags.pickedKaizyle = DialogueFlags.didntPeekKaizyleRoom = true;
                    UpdateDialogueStates(2);
                    break;
                case 68:
                    DialogueFlags.didntPeekKaizyleRoom = false;
                    FlipSpriteAnimation(_kaizyle, false, 10f);
                    AnimationManager.AddNewTransformPositionAnimation(_kaizyle, _rightCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    DialogueFlags.botheredKaizyle = true;
                    break;
                case 69:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleBrag, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
                case 70:
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaShock, Color.white);
                    DialogueFlags.askedKaizyleForHelp = true;
                    UpdateDialogueStates(2);
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
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 78:
                    FlipSpriteAnimation(_soda, false);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1.2f, GetSecondDegreeAnimationFunction(0.5f));
                    ChangeCharSprite(_kaizyleSprite, CharExpressions.KaizyleNeutral, Color.white);
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

                //START CHAPTER 3 
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
                case 109:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieNeutral, Color.white);
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
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 1f, GetSecondDegreeAnimationFunction(0.6f));
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieAnxious, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320001, 2.65f));
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
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f));
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
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outLeftCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _outLeftCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 164, 2.65f));
                    DialogueFlags.walkedTrixieBackHome = true;
                    break;
                case 164:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f));
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
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutralTalk, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _rightCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f), delegate
                    {
                        FlipSpriteAnimation(_soda, true, 0.8f);
                    });
                    AnimationManager.AddNewTransformPositionAnimation(_trixiebell, _centerCharPosition, 4f, GetSecondDegreeAnimationFunction(0.1f));
                    break;
                case 173:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 174:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment1, Color.white);
                    break;
                case 175:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaAgree, Color.white);
                    break;
                case 176:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixiePleased, Color.white);
                    break;
                case 177:
                    DialogueFlags.wantsToGoToAquarium = true;
                    UpdateDialogueStates(3);
                    break;
                case 178:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaEmbarrassedLight, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment3, Color.white);
                    break;
                case 179:
                    FlipSpriteAnimation(_soda, false, .8f);
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _outRightCharPosition, 2.75f, GetSecondDegreeAnimationFunction(0.2f));
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    ChangeCharSprite(_trixiebellSprite, CharExpressions.TrixieCompliment2, Color.white);
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 320001, 2.65f));
                    break;
                case 320001:
                    ChangeCharSprite(_beezerlySprite, DialogueFlags.talkedShitAboutRock ? CharExpressions.BeezerlyAggro : CharExpressions.BeezerlyNeutral, Color.white);
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    AnimationManager.AddNewTransformPositionAnimation(_beezerly, _rightCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    AnimationManager.AddNewTransformPositionAnimation(_soda, _leftCenterCharPosition, 1f, GetSecondDegreeAnimationFunction());
                    break;
                case 320002:
                    break;
                case 320003:
                    break;
                case 320004:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 330000, 2.65f)); //to Chap 3 part 3 transition
                    break;
                case 331115:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 340000, 2.65f)); //To Chap 3 part 4 transition
                    break;
                case 350000:
                    Plugin.Instance.StartCoroutine(FadeOutScene(__instance, 410000, 2.65f)); //To Chap 4 transition
                    break;


                case 110401:
                case 110803:
                case 71:
                    ChangeCharSprite(_sodaSprite, CharExpressions.SodaNeutral, Color.white);
                    break;
            }

            return false;
        }

        public static IEnumerator FadeOutScene(DemonDialogue __instance, int nextDialogueID, float delay = 0)
        {

            yield return new WaitForSeconds(delay);

            __instance.csc.fadeoutpanel.transform.localScale = new Vector3(2f, 0.001f, 1f);
            __instance.csc.fadeoutpanel.SetActive(true);
            __instance.csc.fadeMus(0, false);
            __instance.csc.fadeMus(1, false);
            AnimationManager.AddNewTransformScaleAnimation(__instance.csc.fadeoutpanel, new Vector3(2f, 2f, 1f), 2f, GetSecondDegreeAnimationFunction(), delegate
            {
                switch (nextDialogueID)
                {
                    //end chapter 1
                    case 210000:
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("ClassroomEvening.png");
                        _txtBox.UpdateText("");
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
                        __instance.csc.fadeMus(0, true);
                        _txtBox.UpdateText("");
                        break;
                    //Street Night Scene
                    case 156:
                    case 164:
                        _soda.transform.position = _outLeftCharPosition + new Vector3(1, 0, 0);
                        _trixiebell.transform.position = _outLeftCharPosition;
                        FlipSpriteLeftAnimation(_soda, false, 10f);
                        FlipSpriteRightAnimation(_trixiebell, false, 10f);
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

                    //Beezerly's date
                    case 320001:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        LogChapter3Part1States();
                        LogScores();
                        break;

                    //Appaloosa's Date
                    case 330000:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        LogChapter3Part2States();
                        LogScores();
                        break;

                    //Kaizyle Date
                    case 340000:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("MusicRoom.png");
                        LogChapter3Part3States();
                        LogScores();
                        break;

                    //end Chapter 3 part 4
                    case 410000:
                        ResetCharacterPositions();
                        _txtBox.UpdateText("");
                        __instance.csc.demonbg.transform.Find("Image").GetComponent<Image>().sprite = TooterAssetsManager.GetSprite("Backstage.png");
                        LogChapter3Part4States();
                        LogScores();
                        break;
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
            Plugin.Instance.LogInfo("CURRENT CHAPTER2 STATES:");
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
            Plugin.Instance.LogInfo("CURRENT CHAPTER2 STATES:");
            Plugin.Instance.LogInfo("   wentToRockCafe: " + DialogueFlags.wentToRockCafe);
            Plugin.Instance.LogInfo("   orderedBurger: " + DialogueFlags.orderedBurger);
            Plugin.Instance.LogInfo("   agreedWithBeezerly: " + DialogueFlags.agreedWithBeezerly);
            Plugin.Instance.LogInfo("   likedTheBurger: " + DialogueFlags.likedTheBurger);
            Plugin.Instance.LogInfo("-----------------------------");
        }

        public static void LogChapter3Part3States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER2 STATES:");
            Plugin.Instance.LogInfo("   wentToRockCafe: " + DialogueFlags.wentToRockCafe);
            Plugin.Instance.LogInfo("-----------------------------");
        }

        public static void LogChapter3Part4States()
        {
            Plugin.Instance.LogInfo("CURRENT CHAPTER2 STATES:");
            Plugin.Instance.LogInfo("   wentToRockCafe: " + DialogueFlags.wentToRockCafe);
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
                    dialogueText = $"CHAPTER 1: The beginning.",
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
                    dialogueText = $"{_sodaColoredName}: I've been practicing so hard, and I really want to win.",
                    option2DialogueID = 110002
                }
            },
            {110002,
                new DialogueData()
                {
                    dialogueText = $"[Someone else enters the room]",
                    option2DialogueID = 110003,

                }
            },
            {110003,
                new DialogueData()
                {
                    dialogueText = $"???: Oh, sorry. I didn't mean to interrupt. I was just looking for my music sheet.",
                    option2DialogueID = 110004
                }
            },
            {110004,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh, hey {_trixieColoredName}. It's okay, you're not interrupting anything.",
                    option2DialogueID = 110005
                }
            },
            {110005,
                new DialogueData()
                {
                    dialogueText =  $"{_trixieColoredName}: Thanks. I'm really nervous about the competition this year. I don't know if I can do it.",
                    option1Text = "Cheer",
                    option1DialogueID = 110100,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2f
                    },
                    option2Text = "Ignore",
                    option2DialogueID = 110200,
                }
            },
            {110100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Don't worry, {_trixieColoredName}. You're a great player. I'm sure you'll do great.",
                    option2DialogueID = 110200
                }
            },
            {110200,
                new DialogueData()
                {
                    dialogueText = $"[As they continue to chat, the door opens again and another girl walks in]",
                    option2DialogueID = 110201
                }
            },
            {110201,
                new DialogueData()
                {
                    dialogueText = $"???: Hey there, music nerds. What's going on?",
                    option2DialogueID = 110202
                }
            },
            {110202,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_beezerlyColoredName}. Just getting ready for the competition.",
                    option2DialogueID = 110203
                }
            },
            {110203,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Yeah, yeah, the competition. Whatever. I'm just here to jam and have some fun.",
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
                    dialogueText = $"{_sodaColoredName}: Well, everyone has their own way of enjoying music. Maybe we can show her the fun in the competition too.",
                    option2DialogueID = 110401
                }
            },
            {110400,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, everyone has their own way of enjoying music. Having fun should be the top most reason to play music.",
                    option2DialogueID = 110401
                }
            },
            {110401,
                new DialogueData()
                {
                    dialogueText = $"[As they continue to chat, the door opens again as another girl enters the room, carrying a sleek, professional-looking trombone]",
                    option2DialogueID = 110402
                }
            },
            {110402,
                new DialogueData()
                {
                    dialogueText = $"???: Hey, everyone. Is this where the cool trombone players hang out?",
                    option1Text = "Welcome in!",
                    option1DialogueID = 110500,
                    option1Score = new ScoreData()
                    {
                        appaloosaScore = 1,
                        trixieScore = -1,
                    },
                    option2Text = "Guess so...",
                    option2DialogueID = 110600,
                }
            },
            {110500,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Absolutely! What have you been up to?",
                    option2DialogueID = 110601
                }
            },
            {110600,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I guess so. What brings you here?",
                    option2DialogueID = 110601
                }
            },
            {110601,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: I heard there were some talented players in this room, and I wanted to see for myself. I'm {_appaloosaColoredName}, by the way.",
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
                    dialogueText = $"{_sodaColoredName}: Nice to meet you, {_appaloosaColoredName}. These are my bandmates {_trixieColoredName} and {_beezerlyColoredName}.",
                    option2DialogueID = 110701
                }
            },
            {110800,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Nice to meet you, {_appaloosaColoredName}. I'm {_sodaColoredName}!",
                    option2DialogueID = 110801
                }
            },
            {110701,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: Cool names. You guys want to jam sometime?",
                    option2DialogueID = 110802
                }
            },
            {110801,
                new DialogueData()
                {
                    dialogueText = $"{_appaloosaColoredName}: I like your style, {_sodaColoredName}. Want to jam sometime?",
                    option2DialogueID = 110802
                }
            },
            {110802,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Definitely. That would be awesome.",
                    option2DialogueID = 110803
                }
            },
            {110803,
                new DialogueData()
                {
                    dialogueText = $"[The door opens again and another girl enters the room, carrying a fancy, gold-plated trombone]",
                    option2DialogueID = 110804
                }
            },
            {110804,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Good afternoon, everyone. I'm {_kaizyleColoredName}, and I'm here to rehearse for the competition.",
                    option2DialogueID = 110805
                }
            },
            {110805,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Oh great, another snobby classical player.",
                    option2DialogueID = 110806
                }
            },
            {110806,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Excuse me? I beg your pardon, but I come from a long line of respected musicians. I take my craft very seriously.",
                    option1Text = "Calm down",
                    option1DialogueID = 110900,
                    option1Score = new ScoreData()
                    {
                        kaizyleScore = 1,
                    },
                    option2Text = "Welcome",
                    option2DialogueID = 111000,
                    option2Score = new ScoreData()
                    {
                        kaizyleScore = -1,
                        beezerlyScore = 1,
                    },
                }
            },
            {110900,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's nice to meet you, {_kaizyleColoredName}. We're all here for the same reason, right? To make beautiful music?",
                    option2DialogueID = 110901
                }
            },
            {111000,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's nice to meet you, {_kaizyleColoredName}. Lets play some music!",
                    option2DialogueID = 111001
                }
            },
            {110901,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yes, I suppose you're right. Let's get started then.",
                    option2DialogueID = 111002
                }
            },
            {111001,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yes, Let's get started!",
                    option2DialogueID = 111002
                }
            },
            {111002,
                new DialogueData()
                {
                    dialogueText = $"[The characters take their seats and start to practice their instruments]",
                    option2DialogueID = 111003
                }
            },
            {111003,
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
                    dialogueText = $"CHAPTER 2: Talking Tromboning",
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
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                    option2Text = "Stop stressing out",
                    option2DialogueID = 40,
                    option2Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
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
                    dialogueText = $"{_beezerlyColoredName}: I like to mix it up, you know? Sometimes I'll play something that rocks or rolls, and other times I'll play some pop songs. I don't like to be boxed in.",
                    option1DialogueID = 46,
                    option1Text = "I hate rock",
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = -5,
                    },
                    option2DialogueID = 48,
                    option2Text = "I love rock",
                    option2Score = new ScoreData()
                    {
                        beezerlyScore = 2,
                    },
                }
            },
            {46,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Ewww Rock? I prefer listening to real music like Jazz or Classical Music",
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
                    dialogueText = $"{_sodaColoredName}: Rock is really cool! I listen to Rock music all the time.",
                    option1Text = "Come up with an idea",
                    option1DialogueID = 49,
                    option1Score = new ScoreData()
                    {
                        beezerlyScore = 1,
                    },
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
                    dialogueText = DialogueFlags.didntPeekKaizyleRoom && DialogueFlags.didntPeekAppaloosaRoom ? $"{_sodaColoredName} was too indecisive..." : $"{_sodaColoredName} to himself: What should I do now?",
                    option1Text = DialogueFlags.didntPeekAppaloosaRoom ? "" : "Talk to Appaloosa",
                    option1DialogueID = 57,
                    option2Text = DialogueFlags.didntPeekKaizyleRoom ? DialogueFlags.didntPeekAppaloosaRoom ? "..." : "" : "Talk to Kaizyle",
                    option2DialogueID = DialogueFlags.didntPeekKaizyleRoom && DialogueFlags.didntPeekAppaloosaRoom ? 81 : 67,
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
                    dialogueText = $"{_kaizyleColoredName}: I would like to help but I really need to finish my practice. We can talk later.",
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
            #region TrixieDate
            {82,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 3: A little one on one",
                    option2DialogueID = 83,
                }
            },
            {83,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_trixieColoredName}, how's it going?",
                    option2DialogueID = 84,
                }
            },
            {84,
                new DialogueData()
                {
                    dialogueText = DialogueFlags.cheeredTrixie ?
                    $"{_trixieColoredName}: Oh, hi {_sodaColoredName}. I'm great! Just practicing for the big competition." :
                    $"{_trixieColoredName}: Oh, hi {_sodaColoredName}. I'm doing okay, I guess. Just practicing for the big competition.",
                    option2DialogueID = DialogueFlags.cheeredTrixie ? 85 : 86,
                }
            },
            {85,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thanks for cheering me up earlier. It really helped me release some stress and I appreciate it.",
                    option2DialogueID = 86,
                }
            },
            {86,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Yeah, I heard about that.{(DialogueFlags.cheeredTrixie ? " You're really talented on the trombone." : "")}",
                    option2DialogueID = 87,
                }
            },
            {87,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: {(DialogueFlags.cheeredTrixie ? "Thank you. But " : "")}I'm just so nervous about performing in front of a big audience.",
                    option2DialogueID = 88,
                }
            },
            {88,
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} thinking about how to be supportive of her and make her feel better]",
                    option1Text = "Penguin pin",
                    option1DialogueID = DialogueFlags.cheeredTrixie ? 89 : 117,
                    option2Text = "I have to go",
                    option2DialogueID = 123,
                }
            },
            {89,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: That’s an interesting pin you have on your bag, but seems kinda random doesn’t it?",
                    option2DialogueID = 90,
                }
            },
            {90,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Oh, it's a pin from the aquarium. Ever since I was kid I’ve been in love with penguins due to how soft and cuddly they look.",
                    option2DialogueID = 91,
                }
            },
            {91,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: That’s why when my family organized a trip to the aquarium I was so excited! This pin was a souvenir from their gift shop.",
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
                    dialogueText = $"{_sodaColoredName}: Oh I see. Actually I have an idea that might help you relax and have some fun at the same time.",
                    option2DialogueID = 93,
                }
            },
            {93,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I know how much you love penguins, so I thought we could go to a penguin cafe.\nThey have all kinds of penguin-themed treats and decorations.",
                    option2DialogueID = 94,
                }
            },
            {94,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Really? That sounds amazing!",
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
                    dialogueText = $"{_sodaColoredName}: Great! I'll meet you outside after class. And don't worry about the competition for now. Just focus on having a good time with the penguins.",
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
                    dialogueText = $"[Later that day...]",
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
                    dialogueText = $"{_sodaColoredName}: Yeah, they're pretty cute. And check out these penguin-shaped cookies!",
                    option1Text = "Share",
                    option1DialogueID = 102,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 2,
                    },
                    option2Text = "Eat one",
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
                    dialogueText = $"{_sodaColoredName}: I learned some breathing techniques that helped me calm down and focus. Would you like me to show you?",
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
                    dialogueText = $"[{_sodaColoredName} and {_trixieColoredName} practices breathing technique for the next 2 hours]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {109,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Remember, whenever you feel nervous, just take a deep breath and visualize yourself succeeding.",
                    option2DialogueID = 110,
                }
            },
            {110,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: You're already amazing on the trombone. Just be yourself and have fun up there.",
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
                    dialogueText = $"{_trixieColoredName}: I'm glad we are friends {_sodaColoredName}", //FRIEND ZONNNNEDDDDDDDDDDDDDDDDDDD EXDEE
                    option2DialogueID = 116,
                }
            },
            {115,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Yeah, that would be a dream come true.",
                    option2DialogueID = 116,
                }
            },
            {116, //Cafe date ending //150
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} and {_trixieColoredName} had a great time together]",
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
                        trixieScore = -2,
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
                    dialogueText = $"{_trixieColoredName}: Ever since I was kid I’ve been in love with penguins due to how soft and cuddly they look.",
                    option2DialogueID = 91,
                }
            },
            {119,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Yeah I also think it's cute.",
                    option2DialogueID = 120,
                }
            },
            {120,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Yeah...",
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
                    option1Text = "Compliment her",
                    option1DialogueID = 125,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -2,
                    },
                    option2Text = "Leave",
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
                    dialogueText = $"{_sodaColoredName}: I have to go walk my goldfish so I'll have to go...",
                    option2DialogueID = 124,
                }
            },
            {124, //GTFO ending
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Alright you have a good one {_sodaColoredName}!",
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
                    dialogueText = $"{_trixieColoredName}: Uh Thanks? I don't think I changed anything.",
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
                    $"{_trixieColoredName}: You are funny {_sodaColoredName}.": // cringe
                    $"{_trixieColoredName}: That's so sweet {_sodaColoredName}, you're making me blush", //fine
                    option2DialogueID = 129,
                }
            },
            {129,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Its getting pretty late, I should start heading home.",
                    option1Text = "Lets meet again",
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
                    dialogueText = $"{_sodaColoredName}: I had a great time with you! I think we should hang out again some time",
                    option2DialogueID = (DialogueFlags.awkwardMomentWithTrixie && DialogueFlags.toldTrixieAboutTheSmell) ? 131 : 132,
                }
            },
            {131,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Maybe, I'll see if I got time with all the practice for the concert.",
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
                    dialogueText = $"[{_sodaColoredName} grabs a cookie and starts eating it]",
                    option2DialogueID = 104,
                }
            },
            {134,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: No thanks I'm not hungry right now, if you want you can have mine.",
                    option1Text = "Not hungry anymore",
                    option1DialogueID = 135,
                    option1Score = new ScoreData()
                    {
                        trixieScore = -1,
                    },
                    option2Text = "Eat her cookie",
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
                    dialogueText = $"[{_sodaColoredName} eat half of his cookie and throw the other half in the garbage]",
                    option2DialogueID = 104,
                }
            },
            {137,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh thanks! I was starving, so I was going to ask you if I can have your cookie anyways!",
                    option2DialogueID = 138,
                }
            },
            {138,
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} eat both cookies in one bite]",
                    option2DialogueID = 104,
                }
            },
            {139,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: So, the night of the competition, i was at a party, and got dared to wear a clown suit that they had...",
                    option2DialogueID = 140,
                }
            },
            {140,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: The party was in the morning right?",
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
                    dialogueText = $"{_trixieColoredName}: The party was in the morning... right?",
                    option2DialogueID = 143,
                }
            },
            {143,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It may or may not of been 1 hour before the call time...",
                    option2DialogueID = 144,
                }
            },
            {144,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: You at least gave yourself time to get your suit and tie on right???",
                    option2DialogueID = 145,
                }
            },
            {145,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: About that...",
                    option2DialogueID = 146,
                }
            },
            {146,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: ...",
                    option2DialogueID = 147,
                }
            },
            {147,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ...",
                    option2DialogueID = DialogueFlags.trixieAteACookie ? 148 : 129,
                }
            },
            {148,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Soo... Were the cookies good?",
                    option2DialogueID = 149,
                }
            },
            {149,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Yes",
                    option2DialogueID = 129,
                }
            },
            {150,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's pretty dark outside so I will walk you home.",
                    option2DialogueID = (DialogueFlags.awkwardMomentWithTrixie && DialogueFlags.toldTrixieAboutTheSmell) ? 151:162,
                }
            },
            {151,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Thanks for the offer, but my house isn't that far from here so I will be fine.",
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
                    dialogueText = $"{_trixieColoredName}: Seriously {_sodaColoredName}, it's fine. You don't need to walk me home.",
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
                    dialogueText = $"{_sodaColoredName}: PLEASE PLEASE PLEASE PLEASE PLEASE PLEASE",
                    option2DialogueID = 155,
                }
            },
            {155, //Transition to night street image
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Fine.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {156,
                new DialogueData()
                {
                    dialogueText = $"[After a short and silence walk, near {_trixieColoredName}'s house]",
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
            {160, // awkward ending
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: See you tomorrow.",
                    option2Text ="",
                    option2DialogueID = 0,
                }
            },
            {161,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Alright but please be careful. See you tomorrow for school!",
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
                    dialogueText = $"[The two start walking to {_trixieColoredName}'s house.]",
                    option2Text ="",
                    option2DialogueID = 0,
                }
            },
            {164, //Transition to night street image
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Today was fun. I enjoyed my time with you {_trixieColoredName}!",
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
                    dialogueText = $"[The two arrive at {_trixieColoredName}'s house]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {172,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Today was a fun day! We should do this more often.",
                    option2DialogueID = 173,
                }
            },
            {173,
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Absolutely, I had a fun time as well.",
                    option1Text = "Kiss",
                    option1DialogueID =  177,
                    option1Score = new ScoreData()
                    {
                        trixieScore = 10,
                        appaloosaScore = -5,
                        beezerlyScore = -5,
                        kaizyleScore = -5,
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
                    dialogueText = $"{_sodaColoredName}: Have a good night {_trixieColoredName}!",
                    option2DialogueID = 176,
                }
            },
            {176, //Sweet ending
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: Goodnight {_sodaColoredName}. See you tomorrow!",
                    option2DialogueID = 179,
                }
            },
            {177,
                new DialogueData()
                {
                    dialogueText = $"[{_sodaColoredName} approaches {_trixieColoredName} slowly and kisses her cheek]",
                    option2DialogueID = 178,
                }
            },
            {178, //blushes and looks excited
                new DialogueData()
                {
                    dialogueText = $"{_trixieColoredName}: ... Thanks for lovely night {_sodaColoredName}, I'll see you tomorrow!",
                    option2DialogueID = 179,
                }
            },
            {179, // Happy ending
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
                    dialogueText = $"[The next day, at end of class and everyone leaves to go home. {_sodaColoredName} goes to {_beezerlyColoredName} to chat with her]",
                    option2DialogueID = 320002,
                }
            },
            {320002,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_beezerlyColoredName}, I was thinking about checking out the new {(DialogueFlags.talkedShitAboutRock?"cafe":"hard rock cafe")} that just opened up. Would you like to go with me?",
                    option2DialogueID = DialogueFlags.talkedShitAboutRock ? 320003 : 320005,
                }
            },
            {320003,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Sorry I can't go, I have to go to work. ",
                    option2DialogueID = 320004,
                }
            },
            {320004, //Don't talk shit about rock
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Oh okay! I'll see you tomorrow then.",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            {320005,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Hmm, a hard rock cafe? You know that's my kind of scene. Sure, I'll go with you.",
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
                    dialogueText = $"[They arrive at the hard rock cafe and are greeted by loud rock music and a lively crowd]",
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
                    dialogueText = $"{_sodaColoredName}: Yeah, it's definitely a unique experience.",
                    option1Text = "Order food",
                    option1DialogueID = 320100,
                    option2Text = "First time here",
                    option2DialogueID = 320010,

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
                    option2Text = "Listen To The Band",
                    option2DialogueID = 320200, // TODO
                }
            },
            {320100,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'm getting hungry, do you want to order food?",
                    option2DialogueID = 320101,

                }
            },
            {320101,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Hell yeah! What do you want to order?",
                    option1Text = "Burger",
                    option1DialogueID = 320102,
                    option2Text = "Pineapple pizza",
                    option2DialogueID = 320300,

                }
            },
            {320102,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Let's try those burgers and fries you mentioned earlier!",
                    option2DialogueID = 320013,

                }
            },
            {320300,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I have never tried pineapple pizza before so lets try that.",
                    option2DialogueID = 320013,

                }
            },
            {320013,
                new DialogueData()
                {
                    dialogueText = $"[They order {(DialogueFlags.orderedBurger ? "burgers" : "pineapple pizza")} and drinks and settle at a table]",
                    option1Text = "Ask more about her",
                    option1DialogueID = 320014,
                    option2Text = "Ask about the food",
                    option2DialogueID = 320400,
                }
            },
            {320400,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: How do you like the {(DialogueFlags.orderedBurger ? "burgers" : "pineapple pizza")}?",
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
                    dialogueText = $"{_beezerlyColoredName}: I haven't tried it yet but it looks ok.",
                    option2DialogueID = 320014
                }
            },
            {320014,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: So, I know you're not really into the music competition, but I've always wondered why. Is it because you don't think you can win?",
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
                    option2Text = "Agree",
                    option2DialogueID = DialogueFlags.isCompetitive ? 320017 : 320600,
                }
            },
            {320700,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: There is no point in playing music if you're not trying to be the best.",
                    option2DialogueID = 320701 // TODO
                }
            },
            {320800,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Musicians can be compete without hating eachother.",
                    option2DialogueID = 320801
                }
            },
            {320801,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: A friendly rivalery can help people push their limit without tearing them apart.",
                    option2DialogueID = 320802
                }
            },
            {320802,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: You are right {_sodaColoredName}. I never thought about it that way.",
                    option2DialogueID = 320803 // TODO
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
                    dialogueText = $"{_beezerlyColoredName}: I can respect that. You're really dedicated to your craft.",
                    option2DialogueID = 320019,
                }
            },
            {320019,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Thanks. I just really love playing the trombone. And I can tell you love music too, just in a different way.",
                    option2DialogueID = 320020,
                }
            },
            {320020,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Absolutely. I love going to concerts and feeling the energy of the crowd.",
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
                    dialogueText = $"[Their food arrives and they dig in, enjoying the delicious burgers and fries]",
                    option1Text = "Discuss Burger",
                    option1DialogueID = 321000,
                    option2Text = "Compliment Burger",
                    option2DialogueID = 320023
                }
            },
            {321000,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: These burgers are alright, but I’ve had better.",
                    option2DialogueID = 321001
                }
            },
            {321001,
                new DialogueData()
                {
                    dialogueText = $"{_beezerlyColoredName}: Oh? And where would these better burgers be found?",
                    option2DialogueID = 321002 // TODO
                }
            },
            {320023,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: These burgers are amazing. Thanks for suggesting this place.",
                    option2DialogueID = 320024,
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
                    dialogueText = $"[They finish their food and drinks and prepare to leave]",
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
                    dialogueText = $"{_sodaColoredName}: And you're pretty cool for a rebellious trombone player.",
                    option2DialogueID = 320029,
                }
            },
            {320030,
                new DialogueData()
                {
                    dialogueText = $"[They finish their food and drinks and prepare to leave]", // TODO maybe different endings?
                    option2DialogueID = 320030,
                }
            },
            #endregion

            #region Appaloosa Date
            {330000,
                new DialogueData()
                {
                    dialogueText = $"Soda: Hi Appaloosa, thanks for agreeing to give me some trombone lessons. I'm really excited to learn from you.",
                    option2DialogueID = 330001,
                }
            },
            {330001,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: It's my pleasure, Soda. I can tell you're passionate about music, and I love helping others develop their skills. So, how about we start with a few warm-up exercises?",
                    option2DialogueID = 330002,
                }
            },
            {330002,
                new DialogueData()
                {
                    dialogueText = $"Soda: Sounds good to me.",
                    option2DialogueID = 330003
                }
            },
            {330003,
                new DialogueData()
                {
                    dialogueText = $"[They begin practicing, and Appaloosa gives Soda pointers on his technique]",
                    option2DialogueID = 330004
                }
            },
            {330004,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: You're a quick learner, Soda. You've got a lot of potential. Have you ever played at a jazz bar before?",
                    option2DialogueID = 330005
                }
            },
            {330005,
                new DialogueData()
                {
                    dialogueText = $"Soda: No, I haven't. But I'd love to check one out!",
                    option2DialogueID = 330006
                }
            },
            {330006,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: Well, you're in luck. I actually work at a jazz bar not too far from here.",
                    option2DialogueID = 330007
                }
            },
            {330007,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: Would you like to come with me tonight? We can grab a drink and listen to some live music.",
                    option1Text = $"Why not",
                    option1DialogueID = 331002,
                    option2Text = $"Yes",
                    option2DialogueID = 331000,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 1f
                    }
                }
            },
            {331000, // Choice 1; yes
                new DialogueData()
                {
                    dialogueText = $"Soda: That sounds amazing! I'd love to.", // Excited
                    option2DialogueID = 331001,
                }
            },
            {331001,
                new DialogueData()
                {
                    dialogueText = $"[That night, they arrive at the jazz bar, where the music is already in full swing]",
                    option2DialogueID = 331002
                }
            },
            {331002,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: What do you think, Soda? This is the real deal, right?", //Yelling over music
                    option1DialogueID = 331200,
                    option1Text = $"Not really",
                    option2DialogueID = 331100,
                    option2Text = $"Absolutely",
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 1f
                    }
                }
            },
            {331100, //Choice 1; yes
                new DialogueData()
                {
                    dialogueText = $"Soda: Definitely. I can feel the energy in this place.",
                    option2DialogueID = 331101
                }
            },
            {331101,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: That's the power of jazz. It brings people together and creates a sense of community.",
                    option2DialogueID = 331102
                }
            },
            {331102,
                new DialogueData()
                {
                    dialogueText = $"Soda: I never really thought about it that way before!",
                    option2DialogueID = 331103
                }
            },
            {331103,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: That's what I'm here for. To share my love of jazz with others.",
                    option2DialogueID = 331104
                }
            },
            {331104,
                new DialogueData()
                {
                    dialogueText = $"[They order their drinks and settle in at a table near the stage]",
                    option2DialogueID = 331105
                }
            },
            {331105,
                new DialogueData()
                {
                    dialogueText = $"Soda: This is really great, Appaloosa. I can't thank you enough for bringing me here.", // Drinking
                    option2DialogueID = 331106
                }
            },
            {331106,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: It's my pleasure, Soda. You remind me of myself when I was your age. So full of potential and passion.",
                    option1Text = $"Thank",
                    option1DialogueID = 331120,
                    option2Text = $"Flirt",
                    option2DialogueID = 331110,
                    option2Score = new ScoreData
                    {
                        appaloosaScore = 3f
                    }
                }
            },
            {331120,
                new DialogueData()
                {
                    dialogueText = $"Soda: Oh, thanks I guess..",
                    option2DialogueID = 331113
                }
            },
            {33110,
                new DialogueData()
                {
                    dialogueText = $"Soda: Thanks, Appaloosa. That means a lot coming from someone as talented as you.", // Blushing
                    option2DialogueID = 331111

                }
            },
            {331111, // Choice 1; flirt
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: Oh, stop it. But seriously, if you ever want to perform here, just let me know. I'd be happy to help you get started.", // Laugh
                    option2DialogueID = 331112
                }
            },
            {331112,
                new DialogueData()
                {
                    dialogueText = $"Soda: That would be amazing. I'll definitely take you up on that offer.", // Excited
                    option2DialogueID = 331113
                }
            },
            {331113,
                new DialogueData()
                {
                    dialogueText = $"[As the night wears on, they enjoy the music and conversation, and Soda feels grateful for the opportunity to learn from such a talented musician.]",
                    option2DialogueID = 331114
                }
            },
            {331114,
                new DialogueData()
                {
                    dialogueText = $"Soda: This has been such an incredible night, Appaloosa. Thank you again for everything.",
                    option2DialogueID = 331115
                }
            },
            {331115,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: Anytime, Soda",
                    option2Text = "",
                    option2DialogueID = 0
                }
            },
            #endregion

            #region Kaizyle Date
            {340000,
                new DialogueData()
                {
                    dialogueText = $"[It's the end of class and everyone leaves to go home. {_sodaColoredName} goes to Kaizyle to chat with her]",
                    option2DialogueID = 340001,
                }
            },
            {340001,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_kaizyleColoredName}, how's it going?",
                    option2DialogueID = 340002,
                }
            },
            {340002,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: It's going fine, why do you ask?",
                    option2DialogueID = 340003,
                }
            },
            {340003,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, I was thinking about going out for a walk in the park and maybe grabbing some ice cream. Would you like to come with me?",
                    option2DialogueID = 340004,
                }
            },
            {340004,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Why would I want to go out with you?",
                    option2DialogueID = 340005,
                }
            },
            {340005,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Well, I thought it would be nice to spend some time together and get to know each other better.",
                    option2DialogueID = 340006,
                }
            },
            {340006,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: We're in the same music program, and I feel like we haven't had a chance to talk much.",
                    option2DialogueID = 340007,
                }
            },
            {340007,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Hmph, I suppose it wouldn't hurt to get to know my fellow band members better.",
                    option2DialogueID = 340008,
                }
            },
            {340008,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Great! Since tomorrow is a weekend, I'll meet you downtown tomorrow?",
                    option2DialogueID = 340009,
                }
            },
            {340009,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Sure, whatever.",
                    option2DialogueID = 340010,
                }
            },
            {340010,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey {_kaizyleColoredName}, it's good to see you again. How have you been?",
                    option2DialogueID = 340011,
                }
            },
            {340011,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I've been fine.",
                    option2DialogueID = 340012,
                }
            },
            {340012,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: ...",
                    option2DialogueID = 340013,
                }
            },
            {340013,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Thanks for inviting me out today.",
                    option2DialogueID = 340014,
                }
            },
            {340014,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: No problem, I thought it would be nice to get some fresh air and enjoy the sunshine. So, what do you want to do?",
                    option2DialogueID = 340015,
                }
            },
            {340015,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: I don't know, it's up to you.",
                    option2DialogueID = 340016,
                }
            },
            {340016,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: How about we go grab some ice cream? There's a really good place down the street.",
                    option2DialogueID = 340017,
                }
            },
            {340017,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Fine, but only because I'm craving something sweet.",
                    option2DialogueID = 340018,
                }
            },
            {340018,
                new DialogueData()
                {
                    dialogueText = $"[They walk to the ice cream shop and order their treats]",
                    option2DialogueID = 340019,
                }
            },
            {340019,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: So, how's your music going? I heard you had a big performance last week.",
                    option2DialogueID = 340020,
                }
            },
            {340020,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: It went well. I played the solo in the third movement of the Mozart Requiem. It was a challenge, but I think I nailed it.",
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
                    option2DialogueID = 340023,
                }
            },
            {340023,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: I'd like that. You know, I've always admired your talent and dedication to your craft. It's inspiring.",
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
                    dialogueText = $"[They finish their ice cream and start walking back to the park]",
                    option2DialogueID = 340026,
                }
            },
            {340026,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: So, what kind of music do you like to listen to in your free time?",
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
                    dialogueText = $"{_kaizyleColoredName}: I wouldn't have guessed that from your taste in clothes.",
                    option2DialogueID = 340030,
                }
            },
            {340030,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Hey, I like to keep people guessing.",
                    option2DialogueID = 340031,
                }
            },
            {340031,
                new DialogueData()
                {
                    dialogueText = $"[They arrive back at the park and sit down on a bench]",
                    option2DialogueID = 340032,
                }
            },
            {340032,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: It's been really nice spending time with you today, {_kaizyleColoredName}. I feel like I've gotten to know you better.",
                    option2DialogueID = 340033,
                }
            },
            {340033,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: Yeah, it's been...pleasant.",
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
                    dialogueText = $"{_sodaColoredName}: You know, I've been wanting to ask you something.",
                    option1Text = "???",
                    option1DialogueID = 340100,
                    option2Text = "Ask her out",
                    option2DialogueID = 340036,
                }
            },
            {340036,
                new DialogueData()
                {
                    dialogueText = $"{_sodaColoredName}: Would you like to go out with me again sometime? Maybe we could catch a concert or something.",
                    option2DialogueID = 340037,
                }
            },
            {340037,
                new DialogueData()
                {
                    dialogueText = $"{_kaizyleColoredName}: You're pretty bold, {_sodaColoredName}. I guess I could consider it.",
                    option2DialogueID = 340038,
                }
            },
            {350000,
                new DialogueData()
                {
                    dialogueText = $"[END OF CHAPTER 3]",
                    option2Text = "",
                    option2DialogueID = 0,
                }
            },
            #endregion
        };

        public static Dictionary<int, DialogueData> GetDialogueChapter4() => new Dictionary<int, DialogueData>()
        {
            {410000,
                new DialogueData()
                {
                    dialogueText = $"CHAPTER 4: Concerto Conundrum",
                    option2DialogueID = 410001,
                }
            },
            {410001,
                new DialogueData()
                {
                    dialogueText = $"Soda: Wow, I can't believe the competition is finally here",
                    option2DialogueID = 410002,
                }
            },
            {410002,
                new DialogueData()
                {
                    dialogueText = $"Trixiebell: I know, I'm so scared. What if I mess up?",
                    option2DialogueID = 410003,
                }
            },
            {410003,
                new DialogueData()
                {
                    dialogueText = $"Beezerly : Don't worry about it, guys. Just play from the heart and have fun.",
                    option2DialogueID = 410004,
                }
            },
            {410004,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: That's right. It's not about winning or losing, it's about expressing yourself through music.",
                    option2DialogueID = 410005,
                }
            },
            {410005,
                new DialogueData()
                {
                    dialogueText = $"Kaizyle: I have no doubt that we'll win if we stick to the plan. Let's show them what real musicians can do.",
                    option2DialogueID = 410006,
                }
            },
            {410006,
                new DialogueData()
                {
                    dialogueText = $"Soda: I have to choose which girl to perform with...but who should it be?",
                    option2DialogueID = 410007,
                }
            },
            {410007,
                new DialogueData()
                {
                    dialogueText = $"Trixiebell: Soda, will you perform with me?",
                    option1Text = "Perform With Trixie",
                    option1DialogueID = 410100,
                    option2Text = "Deny Offer",
                    option2DialogueID = 410008,
                }
            },
            {410100, //transition to loading the song for trixiebell
                new DialogueData()
                {
                    dialogueText = $"[Soda and Trixiebell are getting ready to perform together]",
                    option2DialogueID = 0,
                }
            },
            {410008,
                new DialogueData()
                {
                    dialogueText = $"Beezerly : How about me??",
                    option1Text = "Perform With Beezerly",
                    option1DialogueID = 410200,
                    option2Text = "Deny Offer",
                    option2DialogueID = 410009,
                }
            },
            {410200, //transition to loading the song for Beezerly
                new DialogueData()
                {
                    dialogueText = $"[Soda and Beezerly are getting ready to perform together]",
                    option2DialogueID = 0,
                }
            },
            {410009,
                new DialogueData()
                {
                    dialogueText = $"Appaloosa: I'd love to perform with you, Soda. What about you?",
                    option1Text = "Perform With Appaloosa",
                    option1DialogueID = 410300,
                    option2Text = "Deny Offer",
                    option2DialogueID = 410010,
                }
            },
            {410300, //transition to loading the song for Appaloosa
                new DialogueData()
                {
                    dialogueText = $"[Soda and Appaloosa are getting ready to perform together]",
                    option2DialogueID = 0,
                }
            },
            {410010,
                new DialogueData()
                {
                    dialogueText = $"Kaizyle: Hurry up and choose already, Soda. We don't have all day.",
                    option1Text = "Perform With Kaizyle",
                    option1DialogueID = 410400,
                    option2Text = "Deny Offer",
                    option2DialogueID = 410011,
                }
            },
            {410400, //transition to loading the song for Kaizyle
                new DialogueData()
                {
                    dialogueText = $"[Soda and Kaizyle are getting ready to perform together]",
                    option2DialogueID = 0,
                }
            },
            {410011,
                new DialogueData()
                {
                    dialogueText = $"Soda: I couldn't choose who to perform with... So I will be perfoming solo!",
                    option2DialogueID = 410012,
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
            public static bool wentToRockCafe;
            public static bool orderedBurger;
            public static bool agreedWithBeezerly;
            public static bool likedTheBurger;
            #endregion

            #region Chapter 3 part 3

            #endregion

            #region Chapter 3 part 4

            #endregion

            #region Chapter 4
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

        public static void ChangeCharSprite(SpriteRenderer renderer, CharExpressions expression, Color? color)
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

            AppaloosaNeutral,
            AppaloosaNeutralTalk,

            BeezerlyNeutral,
            BeezerlyNeutralTalk,
            BeezerlyAggro,
            BeezerlyMock,
            BeezerlyBump,
            BeezerlyImpressed,
            BeezerlyThinking,

            KaizyleNeutral,
            KaizyleNeutralTalk,
            KaizyleDispleased,
            KaizyleWTF,
            KaizyleConcern,
            KaizyleBrag,
            KaizylePissed,
            KaizyleUm,


        }
    }
}
