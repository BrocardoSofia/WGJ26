using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameLanguage
{
    Spanish = 0,
    English = 1
}

public static class GameLocalization
{
    private const string LanguageKey = "settings.language";

    public static event Action LanguageChanged;

    public static GameLanguage CurrentLanguage =>
        (GameLanguage)PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.Spanish);

    public static bool IsSpanish => CurrentLanguage == GameLanguage.Spanish;

    public static void SetLanguage(GameLanguage language)
    {
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
        LocalizeAllLoadedTexts();
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        return IsSpanish ? GetSpanish(key) : GetEnglish(key);
    }

    public static string TranslateText(string value)
    {
        return TranslateKnownText(value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LocalizeAfterSceneLoad()
    {
        LocalizeAllLoadedTexts();
    }

    public static void LocalizeAllLoadedTexts()
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                    text.text = TranslateKnownText(text.text);
            }
        }
    }

    private static string TranslateKnownText(string value)
    {
        string normalizedValue = value.Trim();

        if (normalizedValue.StartsWith("¡OH NO!") || normalizedValue.StartsWith("OH NO!"))
            return Get("intro.story");

        if (normalizedValue.StartsWith("Durante tu viaje estresante") || normalizedValue.StartsWith("During your stressful journey"))
            return Get("intro.strangers");

        if (normalizedValue.StartsWith("Para evitar las conversaciones indeseables") || normalizedValue.StartsWith("To avoid unwanted conversations"))
            return Get("intro.avoidConversations");

        if (normalizedValue.StartsWith("Tomar un café te animará") || normalizedValue.StartsWith("Drinking coffee will help you"))
            return Get("intro.coffee");

        return normalizedValue switch
        {
            "Iniciar juego" or "Start game" => Get("menu.start"),
            "Configuraciones" or "Settings" => Get("menu.settings"),
            "Créditos" or "Credits" => Get("menu.credits"),
            "Salir" or "Exit" => Get("menu.exit"),
            "Siguiente" or "Next" => Get("intro.next"),
            "IMPORTANTE" or "IMPORTANT" => Get("intro.important"),
            "CONTROL" => Get("intro.control"),
            "Elige el modo de control para el juego" or "Choose the control mode for the game" => Get("intro.chooseControl"),
            "Di 'No' con el microfono" or "Say 'No' into the microphone" => Get("intro.sayNoMicrophone"),
            "Di 'No' con la barra espaciadora" or "Press the space bar to say 'No'" => Get("intro.sayNoKeyboard"),
            "Menú" or "Menu" => Get("pause.menu"),
            "PAUSA" or "PAUSED" => Get("pause.title"),
            "Reanudar" or "Resume" => Get("pause.resume"),
            "Volver al menú principal" or "Return to main menu" => Get("pause.mainMenu"),
            "Reconocimiento de voz" or "Voice recognition" => Get("voice.title"),
            "Cambiar micrófono en Windows" or "Change microphone in Windows" => Get("settings.changeMicrophone"),
            "MODO VOZ" or "VOICE MODE" => Get("voice.modeVoice"),
            "MODO TECLADO" or "KEYBOARD MODE" => Get("voice.modeKeyboard"),
            "Volver a intentar" or "Try again" => Get("game.retry"),
            "Volver al menú" or "Return to menu" => Get("game.menu"),
            "GAME OVER" => Get("game.gameOverTitle"),
            "GANASTE" or "YOU WIN" => Get("game.win"),
            "Llegaste a tiempo al trabajo" or "You got to work on time" => Get("game.winReason"),
            "Ir a créditos" or "Go to credits" => Get("game.credits"),
            "Volver a jugar" or "Play again" => Get("game.playAgain"),
            "Por sobredosis de café" or "Coffee overdose" => Get("game.coffeeOverReason"),
            "No llegaste a tiempo al trabajo" or "You did not get to work on time" => Get("game.timeOverReason"),
            "Control por voz (desactivar: teclado)" or "Voice control (disable: keyboard)" => Get("voice.legacyControl"),
            "GAME OVER\nPor sobredosis de café" or "GAME OVER\nCoffee overdose" => Get("game.coffeeOver"),
            "GAME OVER\nNo llegaste a tiempo al trabajo" or "GAME OVER\nYou did not get to work on time" => Get("game.timeOver"),
            _ => value
        };
    }

    private static string GetSpanish(string key)
    {
        return key switch
        {
            "menu.start" => "Iniciar juego",
            "menu.settings" => "Configuraciones",
            "menu.credits" => "Créditos",
            "menu.exit" => "Salir",
            "intro.next" => "Siguiente",
            "intro.story" => "¡OH NO!\n\n¡Pasaste demasiado tiempo anoche deslizándote por TikTok y te has despertado tarde!\n\n¡El tiempo para llegar al trabajo está contado! Y encima de todo esto hay un reto más…\n\ndecirle “no” a los desconocidos.",
            "intro.strangers" => "Durante tu viaje estresante, varios desconocidos intentarán hablar contigo; parar a hablar con los personajes demorará el tiempo limitado que tienes para llegar a tu trabajo.",
            "intro.avoidConversations" => "Para evitar las conversaciones indeseables, di “NO” o toca la barra espaciadora.",
            "intro.coffee" => "Tomar un café te animará a llegar más rápido al trabajo, ¡pero ojo! Si tomas más de tres cafés nunca más trabajarás…",
            "intro.important" => "IMPORTANTE",
            "intro.control" => "CONTROL",
            "intro.chooseControl" => "Elige el modo de control para el juego",
            "intro.sayNoMicrophone" => "Di 'No' con el microfono",
            "intro.sayNoKeyboard" => "Di 'No' con la barra espaciadora",
            "pause.menu" => "Menú",
            "pause.title" => "PAUSA",
            "pause.resume" => "Reanudar",
            "pause.mainMenu" => "Volver al menú principal",
            "settings.title" => "CONFIGURACIONES",
            "settings.microphone" => "Micrófono predeterminado de Windows\n{0}",
            "settings.noMicrophone" => "No se detectó ningún micrófono",
            "settings.changeMicrophone" => "Cambiar micrófono en Windows",
            "settings.control" => "Control",
            "settings.voice" => "Micrófono",
            "settings.keyboard" => "Barra espaciadora",
            "settings.music" => "Volumen de música",
            "settings.effects" => "Volumen de efectos",
            "settings.resolution" => "Resolución",
            "settings.fullscreen" => "Pantalla completa",
            "settings.language" => "Idioma",
            "settings.spanish" => "Español",
            "settings.english" => "Inglés",
            "voice.title" => "Reconocimiento de voz",
            "voice.defaultMicrophone" => "Micrófono: entrada predeterminada de Windows",
            "voice.pausedMicrophone" => "Micrófono: predeterminado de Windows (pausado)",
            "voice.unavailable" => "Reconocimiento disponible solamente en Windows.",
            "voice.startError" => "No se pudo iniciar Windows Speech.",
            "voice.detectedOne" => "Detectado: No",
            "voice.detectedMany" => "Detectados: {0} × No",
            "voice.modeVoice" => "MODO VOZ",
            "voice.modeKeyboard" => "MODO TECLADO",
            "voice.legacyControl" => "Control por voz (desactivar: teclado)",
            "game.retry" => "Volver a intentar",
            "game.menu" => "Volver al menú",
            "game.gameOverTitle" => "GAME OVER",
            "game.win" => "GANASTE",
            "game.winReason" => "Llegaste a tiempo al trabajo",
            "game.credits" => "Ir a créditos",
            "game.playAgain" => "Volver a jugar",
            "game.coffeeOverReason" => "Por sobredosis de café",
            "game.timeOverReason" => "No llegaste a tiempo al trabajo",
            "game.coffeeOver" => "GAME OVER\nPor sobredosis de café",
            "game.timeOver" => "GAME OVER\nNo llegaste a tiempo al trabajo",
            "game.goal" => "{0:0.0} mts",
            "game.time" => "{0:00}:{1:00}",
            "credits.title" => "¡Gracias por jugar nuestro juego!",
            "credits.message" => "En un mundo lleno de personas que suelen priorizar las necesidades de los demás antes que las propias, nuestro juego busca reflexionar sobre lo difícil que puede ser decir “no”, establecer límites y encontrar un equilibrio entre acompañar a los demás y cuidarse a uno mismo.\n\nGracias a nuestro increíble equipo, lleno de superestrellas amables y colaborativas. ¡Sin ustedes, este lindo juego no habría sido posible!",
            "credits.team" => "Programación, diseño de nivel y producción — {0}\nUI, ilustraciones y animaciones — {1} y {2}\nSFX y música — {3} y {4}",
            "credits.music" => "Música: “Three Red Hearts” por Abstraction\nPistas utilizadas: Box Jump · Go · Modern Bits\ntallbeard.itch.io/three-red-hearts-prepare-to-dev\n\nReconocimiento de voz: Vosk por Alpha Cephei\nModelo: vosk-model-small-es-0.42\nLicencia Apache 2.0 · alphacephei.com/vosk",
            _ => key
        };
    }

    private static string GetEnglish(string key)
    {
        return key switch
        {
            "menu.start" => "Start game",
            "menu.settings" => "Settings",
            "menu.credits" => "Credits",
            "menu.exit" => "Exit",
            "intro.next" => "Next",
            "intro.story" => "OH NO!\n\nYou spent too much time scrolling through TikTok last night and woke up late!\n\nTime is running out to get to work! And on top of all that, there is one more challenge…\n\nsaying “no” to strangers.",
            "intro.strangers" => "During your stressful journey, several strangers will try to talk to you. Stopping to talk to them will use up the limited time you have to get to work.",
            "intro.avoidConversations" => "To avoid unwanted conversations, say “NO” or press the space bar.",
            "intro.coffee" => "Drinking coffee will help you get to work faster, but be careful! If you drink more than three coffees, you will never work again…",
            "intro.important" => "IMPORTANT",
            "intro.control" => "CONTROL",
            "intro.chooseControl" => "Choose the control mode for the game",
            "intro.sayNoMicrophone" => "Say 'No' into the microphone",
            "intro.sayNoKeyboard" => "Press the space bar to say 'No'",
            "pause.menu" => "Menu",
            "pause.title" => "PAUSED",
            "pause.resume" => "Resume",
            "pause.mainMenu" => "Return to main menu",
            "settings.title" => "SETTINGS",
            "settings.microphone" => "Windows default microphone\n{0}",
            "settings.noMicrophone" => "No microphone detected",
            "settings.changeMicrophone" => "Change microphone in Windows",
            "settings.control" => "Control",
            "settings.voice" => "Microphone",
            "settings.keyboard" => "Space bar",
            "settings.music" => "Music volume",
            "settings.effects" => "Effects volume",
            "settings.resolution" => "Resolution",
            "settings.fullscreen" => "Fullscreen",
            "settings.language" => "Language",
            "settings.spanish" => "Spanish",
            "settings.english" => "English",
            "voice.title" => "Voice recognition",
            "voice.defaultMicrophone" => "Microphone: Windows default input",
            "voice.pausedMicrophone" => "Microphone: Windows default input (paused)",
            "voice.unavailable" => "Voice recognition is only available on Windows.",
            "voice.startError" => "Windows Speech could not be started.",
            "voice.detectedOne" => "Detected: No",
            "voice.detectedMany" => "Detected: {0} × No",
            "voice.modeVoice" => "VOICE MODE",
            "voice.modeKeyboard" => "KEYBOARD MODE",
            "voice.legacyControl" => "Voice control (disable: keyboard)",
            "game.retry" => "Try again",
            "game.menu" => "Return to menu",
            "game.gameOverTitle" => "GAME OVER",
            "game.win" => "YOU WIN",
            "game.winReason" => "You got to work on time",
            "game.credits" => "Go to credits",
            "game.playAgain" => "Play again",
            "game.coffeeOverReason" => "Coffee overdose",
            "game.timeOverReason" => "You did not get to work on time",
            "game.coffeeOver" => "GAME OVER\nCoffee overdose",
            "game.timeOver" => "GAME OVER\nYou did not get to work on time",
            "game.goal" => "{0:0.0} m",
            "game.time" => "{0:00}:{1:00}",
            "credits.title" => "Thank you for playing our game!",
            "credits.message" => "In a world full of people who often prioritize other people's needs over their own, our game invites reflection on how difficult it can be to say “no,” set boundaries, and find a balance between supporting others and taking care of yourself.\n\nThanks to our amazing team of kind and collaborative superstars. Without all of you, this lovely game would not have been possible!",
            "credits.team" => "Programming, level design and production — {0}\nUI, illustrations and animation — {1} and {2}\nSFX and music — {3} and {4}",
            "credits.music" => "Music: “Three Red Hearts” by Abstraction\nTracks used: Box Jump · Go · Modern Bits\ntallbeard.itch.io/three-red-hearts-prepare-to-dev\n\nVoice recognition: Vosk by Alpha Cephei\nModel: vosk-model-small-es-0.42\nApache License 2.0 · alphacephei.com/vosk",
            _ => key
        };
    }
}
