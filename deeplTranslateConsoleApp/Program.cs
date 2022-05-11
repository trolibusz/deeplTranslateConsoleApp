using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using DeepL;
using dotenv.net;

namespace deeplTranslateConsoleApp
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var envVars = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 4, ignoreExceptions: false));
                string APIKey = envVars["APIKey"];
                DeepL.Translator translator = new Translator(APIKey);
                List<DeepL.Model.TargetLanguage> targetLangList = new List<DeepL.Model.TargetLanguage>();
                List<DeepL.Model.SourceLanguage> sourceLangList = new List<DeepL.Model.SourceLanguage>();

                Console.WriteLine("Loading...");

                await getLangCodesAsync(translator, targetLangList, sourceLangList);

                bool showMenu = true;
                while (showMenu)
                {
                    showMenu = await mainMenuAsync(translator, targetLangList, sourceLangList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ReadKey();
            }
        }

        private static async Task<bool> mainMenuAsync(DeepL.Translator translator, List<DeepL.Model.TargetLanguage> targetLangList, List<DeepL.Model.SourceLanguage> sourceLangList)
        {
            Console.Clear();
            Console.WriteLine("DeepL Translate Console Edition");
            Console.WriteLine("\nChoose an option:\n");
            Console.WriteLine("1) Translate");
            Console.WriteLine("2) Translate a document");
            Console.WriteLine("3) Available characters this month with free API");
            Console.WriteLine("4) Languages");
            Console.WriteLine("5) Exit");
            Console.Write("\r\nOption: ");

            switch (Console.ReadLine())
            {
                case "1":
                    await translate(translator, targetLangList, sourceLangList);
                    return true;
                case "2":
                    await translateDocument(translator, targetLangList, sourceLangList);
                    return true;
                case "3":
                    await checkAccountUsageAsync(translator);
                    return true;
                case "4":
                    printLangCodesAsync(targetLangList, sourceLangList);
                    return true;
                case "5":
                    return false;
                default:
                    return true;
            }
        }

        private static async Task translate(DeepL.Translator translator, List<DeepL.Model.TargetLanguage> targetLangList, List<DeepL.Model.SourceLanguage> sourceLangList)
        {
            try
            {
                Console.Clear();

                Console.Write("Language you want to translate from: ");
                string sourceLangInput = Console.ReadLine();

                while (!isInSourceLangList(sourceLangInput, sourceLangList))
                {
                    Console.Write("Language you want to translate from: ");
                    sourceLangInput = Console.ReadLine();
                }

                Console.Write("Language you want to translate to: ");
                string targetLangInput = Console.ReadLine();

                while (!isInTragetLangList(targetLangInput, targetLangList))
                {
                    Console.Write("Language you want to translate to: ");
                    targetLangInput = Console.ReadLine();
                }

                Console.Write("Enter what you want to translate: ");
                string needsToBeTranslated = Console.ReadLine();

                var translatedText = await translator.TranslateTextAsync(needsToBeTranslated, sourceLangInput, targetLangInput);

                Console.WriteLine($"Translated text: {translatedText.Text}");

                exitToMainMenu();
            }
            catch (Exception ex)
            {
                Console.Write("ERROR: " + ex.Message);
                exitToMainMenu();
            }
        }

        private static async Task translateDocument(DeepL.Translator translator, List<DeepL.Model.TargetLanguage> targetLangList, List<DeepL.Model.SourceLanguage> sourceLangList)
        {
            try
            {
                Console.Clear();

                Console.Write("Language you want to translate from: ");
                string sourceLangInput = Console.ReadLine();

                while (!isInSourceLangList(sourceLangInput, sourceLangList))
                {
                    Console.Write("Language you want to translate from: ");
                    sourceLangInput = Console.ReadLine();
                }

                Console.Write("Language you want to translate to: ");
                string targetLangInput = Console.ReadLine();

                while (!isInTragetLangList(targetLangInput, targetLangList))
                {
                    Console.Write("Language you want to translate to: ");
                    targetLangInput = Console.ReadLine();
                }

                Console.Write("Enter the document's name you want to translate (with extension): ");
                FileInfo inputFile = new FileInfo(Console.ReadLine());

                Console.Write("Enter the output document's name (with extension): ");
                FileInfo outputFile = new FileInfo(Console.ReadLine());

                await uploadDocument(translator, sourceLangInput, targetLangInput, inputFile, outputFile);

                exitToMainMenu();
            }
            catch (Exception ex)
            {
                Console.Write("ERROR: " + ex.Message);
                exitToMainMenu();
            }
        }

        private static async Task uploadDocument(Translator translator, string sourceLangInput, string targetLangInput, FileInfo inputFile, FileInfo outputFile)
        {
            try
            {
                await translator.TranslateDocumentAsync(inputFile, outputFile, sourceLangInput, targetLangInput);
            }
            catch (DocumentTranslationException exception)
            {
                if (exception.DocumentHandle != null)
                {
                    var handle = exception.DocumentHandle.Value;
                    Console.WriteLine($"Document ID: {handle.DocumentId}, Document key: {handle.DocumentKey}");
                    exitToMainMenu();
                }
                else
                {
                    Console.WriteLine($"Error occurred during document upload: {exception.Message}");
                    exitToMainMenu();
                }
            }
        }

        private static async Task checkAccountUsageAsync(DeepL.Translator translator)
        {
            Console.Clear();

            var usage = await translator.GetUsageAsync();

            if (usage.AnyLimitReached)
            {
                Console.WriteLine("Translation limit exceeded.");
            }
            else if (usage.Character != null)
            {
                Console.WriteLine($"Character usage: {usage.Character}");
            }
            else
            {
                Console.WriteLine($"{usage}");
            }

            exitToMainMenu();

        }

        private static async Task getLangCodesAsync(DeepL.Translator translator, List<DeepL.Model.TargetLanguage> targetLangList, List<DeepL.Model.SourceLanguage> sourceLangList)
        {
            var sourceLanguages = await translator.GetSourceLanguagesAsync();

            foreach (var lang in sourceLanguages)
            {
                sourceLangList.Add(lang);
            }

            var targetLanguages = await translator.GetTargetLanguagesAsync();

            foreach (var lang in targetLanguages)
            {
                targetLangList.Add(lang);
            }
        }

        private static void printLangCodesAsync(List<DeepL.Model.TargetLanguage> targetLangList, List<DeepL.Model.SourceLanguage> sourceLangList)
        {
            Console.Clear();

            Console.WriteLine("Source languages:\n");

            foreach (var lang in sourceLangList)
            {
                Console.WriteLine($"{lang.Name} ({lang.Code})");
            }

            Console.WriteLine("\nTarget languages:\n");

            foreach (var lang in targetLangList)
            {
                if (lang.SupportsFormality)
                {
                    Console.WriteLine($"{lang.Name} ({lang.Code}) supports formality.");
                }
                else
                {
                    Console.WriteLine($"{lang.Name} ({lang.Code})");
                }
            }

            exitToMainMenu();
        }

        private static void exitToMainMenu()
        {
            Console.Write("\nPress any key to go back to main menu.");
            Console.ReadKey();
        }

        private static bool isInTragetLangList(string input, List<DeepL.Model.TargetLanguage> targetLangList)
        {
            var langCodes = targetLangList.Select(x => x.Code.ToUpper());

            if (langCodes.Contains(input.ToUpper()))
            {
                return true;
            }

            return false;
        }

        private static bool isInSourceLangList(string input, List<DeepL.Model.SourceLanguage> sourceLangList)
        {
            var langCodes = sourceLangList.Select(x => x.Code.ToUpper());

            if (langCodes.Contains(input.ToUpper()))
            {
                return true;
            }

            return false;
        }
    }
}
