using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Printers.Extensions;
using Windows.Gaming.Input.Custom;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TypingTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private void setupWindow()
        {            
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }
        public MainPage()
        {
            this.InitializeComponent();
            setupWindow();
            testView = new TestTextView(testTextBlock);
            resetButton_Click(null, null);
            createMenus();
        }
        ~MainPage()
        {
            if(cts != null) cts.Cancel();
            if (timerThread.IsAlive) timerThread.Join();
        }

        private String[] testWords;
        private Random charRand = new Random();
        private Random wordRand = new Random();
        enum TestOption
        {
            Random,
            Word1k,
            Word100k,
            Word500k
        }
        enum ClockOption
        {
            OneMinute,
            ThreeMinute,
            FiveMinute,
            TenMinute
        }
        private MenuOptions<TestOption> testOptions;
        private MenuOptions<ClockOption> clockOptions;
        private TestOption testOption = TestOption.Word1k;
        private void createMenus()
        {
            testOptions = new MenuOptions<TestOption>("testOptionMenu", menuFlyout, option => {
                testOption = option;
                resetButton_Click(null, null);
            });
            testOptions.addOption(TestOption.Random, "Random gibberish words", false);
            testOptions.addOption(TestOption.Word1k, "Random words (most used 1k)", true);
            testOptions.addOption(TestOption.Word100k, "Random Words (100k)", false);
            testOptions.addOption(TestOption.Word500k, "Random Words (500k)", false);
            testOptions.updateMenu();
            clockOptions = new MenuOptions<ClockOption>("clockOptionMenu", clockMenuFlyout, option =>
            {
                switch (option)
                {
                    case ClockOption.OneMinute:
                        timerDuration = 60 * 1000;
                        break;
                    case ClockOption.ThreeMinute:
                        timerDuration = 3 * 60 * 1000;
                        break;
                    case ClockOption.FiveMinute:
                        timerDuration = 5 * 60 * 1000;
                        break;
                    case ClockOption.TenMinute:
                        timerDuration = 10 * 60 * 1000;
                        break;
                }
            });
            clockOptions.addOption(ClockOption.OneMinute, "One minute test", true);
            clockOptions.addOption(ClockOption.ThreeMinute, "Three minute test", false);
            clockOptions.addOption(ClockOption.FiveMinute, "Five minute test", false);
            clockOptions.addOption(ClockOption.TenMinute, "Ten minute test", false);
            clockOptions.updateMenu();
        }

        private char getRandomChar(bool withSpecial = false)
        {
            var alphabets = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var numbers = "1234567890";
            var commonChars = ".,;:!?'\"-";
            //String specialChars = "$%#@!*^&";
            var r = charRand.Next(0, 1000);
            if (r < 48)
                return commonChars[charRand.Next(0, commonChars.Length - 1)];
            if (r < 60)
                return numbers[charRand.Next(0, numbers.Length - 1)];
            return alphabets[charRand.Next(0, alphabets.Length - 1)];
        }
        private String[] words1k = null, words100k = null, words500k = null;
        private async Task<String[]> getWordsFromFile(String path)
        {
            String[] words = (await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///" + path)))).Split("\n")
                             .Where(word => !(String.IsNullOrEmpty(word) || word == " " || word.Contains("#!comment:"))).ToArray();
            Debug.WriteLine("Done reading words");
            return words;
        }
        bool capitalNext = false;
        bool invertedCommaRunning = false;
        int closeAfter = 0;
        private String getRandomWord(String[] words)
        {
            String word = words[wordRand.Next(0, words.Length - 1)];
            if (capitalNext)
            {
                word = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word);
                capitalNext = false;
            }
            int r = wordRand.Next(0, 1000);
            if (r < 65)
            {
                word += ".";
                capitalNext = true;
            }
            else if (r < 127) word += ",";
            else if (r < 130) word += ";";
            else if (r < 133) word += ":";
            else if (r < 136) word += "!";
            else if (r < 142) word += "?";
            else if (r < 150)
            {
                invertedCommaRunning = true;
                word = "\"" + word;
                closeAfter = wordRand.Next(5, 15);
            }
            if (invertedCommaRunning && (--closeAfter) <= 0)
            {
                invertedCommaRunning = false;
                word += "\"";
            }
            return word;
        }
        private async Task generateRandomTestText(int count)
        {
            var rand = new Random();
            String[] __testwords = new String[count];
            if(testOption == TestOption.Random)
            {
                for (int i = 0; i < count; i++)
                {
                    String word = "";
                    for (int j = 0; j < rand.Next(1, 10); j++)
                       word += getRandomChar();
                    __testwords[i] = word;
                }
            }
            else
            {
                String[] words;
                switch (testOption)
                {                                     
                    case TestOption.Word100k:
                        if (words100k == null)
                            words100k = await getWordsFromFile("Assets/100k.txt");
                        words = words100k;
                        break;
                    case TestOption.Word500k:
                        if (words500k == null)
                            words500k = await getWordsFromFile("Assets/500k.txt");
                        words = words500k;
                        break;
                    case TestOption.Word1k:
                    default:
                        if (words1k == null)
                            words1k = await getWordsFromFile("Assets/1k.txt");
                        words = words1k;
                        break;
                }
                for (int i = 0; i < count; i++)
                {
                    __testwords[i] = getRandomWord(words);
                }
            }
            testWords = __testwords;
            Debug.WriteLine("Words Processed: {0}/{1}", testWords.Length, count);
        }
        private int totalKeystrokes;
        private Status status = new Status();
        private bool isSinglePunctuation(String word)
        {
            if (word.Length != 1) return false;
            String punctuations = ",;:!?\"";
            foreach (char c in punctuations)
                if (c.ToString() == word)
                    return true;
            return false;
        }
        private Brush Red { get { return new SolidColorBrush(Color.FromArgb(255, 255,0, 0)); } }
        private Brush Blue { get { return new SolidColorBrush(Color.FromArgb(255, 0,255, 255)); } }
        private Brush Green { get { return new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)); } }

        /*
        private void updateTestBlock()
        {
            var enteredWords = editText.Text.Split(" ").Where(word => !(String.IsNullOrEmpty(word) || word == " " || isSinglePunctuation(word))).ToArray();
            var isTypingWord = !String.IsNullOrEmpty(editText.Text) && editText.Text.Last() != ' ';
            testTextBlock.Blocks.Clear();
            var para = new Paragraph();
            para.TextAlignment = TextAlignment.Justify;
            int correctKeyStrokes = 0;
            int wrongWords = 0;
            int correctWords = 0;
            for (int i = 0; i < testWords.Length; i++)
            {
                var run = new Run();
                run.Text = testWords[i];
                if (i >= enteredWords.Length - (isTypingWord ? 1 : 0))
                {
                    run.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    if(isTypingWord && i == (enteredWords.Length - 1))
                    {
                        int j;
                        for (j = 0; j < Math.Min(testWords[i].Length, enteredWords[i].Length); j++)
                        {
                            if (testWords[i][j] == enteredWords[i][j])                            
                                correctKeyStrokes++;
                            else break;
                        }
                        int k = enteredWords[i].Length;
                        String word = testWords[i];
                        var highlightRun = new Run();
                        if(j == word.Length && (k > j))
                            highlightRun.Foreground = Red;
                        else highlightRun.Foreground = Green;
                        highlightRun.Text = word.Substring(0, j);
                        para.Inlines.Add(highlightRun);
                        word = word.Substring(j);
                        if (j < testWords[i].Length)
                        {
                            var cursorRun = new Run();
                            cursorRun.TextDecorations = TextDecorations.Underline;
                            if (j < k)
                            {
                                cursorRun.Foreground = Red;
                                int r = Math.Min(word.Length, k - j);
                                cursorRun.Text = word.Substring(0, r);
                                word = word.Substring(r);
                            }
                            else
                            {
                                cursorRun.Text = word[0].ToString();
                                word = word.Substring(1);
                                cursorRun.FontWeight = FontWeights.Bold;
                                cursorRun.Foreground = Blue;
                            }
                            para.Inlines.Add(cursorRun);
                        }                        
                        run.Text = word;
                    }
                }
                else if (testWords[i] == enteredWords[i])
                {
                    run.Foreground = Green;
                    correctKeyStrokes += testWords[i].Length + 1; //accounting for space
                    correctWords++;
                }
                else
                {
                    run.Foreground = Red;
                    wrongWords++;
                    for (int j = 0; j < Math.Min(testWords[i].Length, enteredWords[i].Length); j++)
                        if (testWords[i][j] == enteredWords[i][j])
                            correctKeyStrokes++;
                    if (testWords[i].Length == enteredWords[i].Length) correctKeyStrokes++; //accounting for space
                }
                para.Inlines.Add(run);
                para.Inlines.Add(new Run { Text = " " });
            }
            testTextBlock.Blocks.Add(para);
            status = new Status
            {
                keystrokeCount = totalKeystrokes,
                correctKeystrokeCount = correctKeyStrokes,
                wrongKeystrokeCount = totalKeystrokes - correctKeyStrokes,
                wordCount = correctWords + wrongWords,
                correctWordCount = correctWords,
                wrongWordCount = wrongWords
            };
        }
        */
        
        private char lastChar;
        private TestTextView testView;
        private int wordStartIndex = 0;
        private bool isTyping = false;
        private bool correctWord = false;
        private int wordLength { get { return editText.Text.Length - 1 - wordStartIndex;  } }
        private void updateTestBlock()
        {
            status.keystrokeCount = totalKeystrokes;
            if (editText.Text.Length == 0)
                testView.update(0, span =>
                {
                    span.textAreas.Add(new TextArea(TextHighlightStyle.Blue, 0, 0));
                    span.textAreas.Add(new TextArea(TextHighlightStyle.Default, 1, testWords[status.wordCount].Length - 1));
                });
            else 
            {
                if (editText.Text.Last() == ' ')
                {
                    if (isTyping && !isKeyBackspace)
                    {
                        if (wordLength != 1 || !isSinglePunctuation(lastChar.ToString()))
                        {
                            status.correctKeystrokeCount++;
                            status.wordCount++;
                            if (correctWord) status.correctWordCount++;
                            else status.wrongWordCount++;
                        }
                    }
                    isTyping = false;
                }
                if (editText.Text.Last() != ' ' || (isKeyBackspace && isTyping))
                {
                    String enteredWord = "";
                    int i;
                    for (i = editText.Text.Length - 1; i >= 0 && editText.Text[i] != ' '; i--)
                        enteredWord = editText.Text[i] + enteredWord;
                    wordStartIndex = i + 1;
                    if (isKeyBackspace)
                    {
                        if (isTyping)
                        {
                            if (enteredWord.Length < testWords[status.wordCount].Length && testWords[status.wordCount][enteredWord.Length] == lastChar) status.correctKeystrokeCount--;
                        }
                        else
                        {
                            status.correctKeystrokeCount--;
                            status.wordCount--;
                            correctWord = testWords[status.wordCount] == enteredWord;
                            if (correctWord) status.correctWordCount--;
                            else status.wrongWordCount--;
                        }
                    }
                    else
                    {
                        if (enteredWord.Length <= testWords[status.wordCount].Length && testWords[status.wordCount][enteredWord.Length - 1] == enteredWord.Last()) status.correctKeystrokeCount++;
                    }
                    isTyping = true;

                    int j, k = enteredWord.Length;
                    for (j = 0; j < Math.Min(testWords[status.wordCount].Length, enteredWord.Length); j++)
                        if (testWords[status.wordCount][j] != enteredWord[j]) break;
                    var word = testWords[status.wordCount];
                    lastChar = editText.Text.Last();
                    correctWord = k == word.Length && word == enteredWord;
                    Debug.WriteLine($"0 - {j - 1}\n{j} - {k - 1}\n{k} - {k}\n{k + 1} - {word.Length - 1}\n{enteredWord}");
                    testView.update(status.wordCount, span => {
                        span.textAreas.Add(new TextArea((correctWord || k <= word.Length) ?
                        TextHighlightStyle.Green : TextHighlightStyle.Red, 0, j - 1));
                        span.textAreas.Add(new TextArea(TextHighlightStyle.Red, j, k - 1));
                        span.textAreas.Add(new TextArea(TextHighlightStyle.Blue, k, k));
                        span.textAreas.Add(new TextArea(TextHighlightStyle.Default, k + 1, word.Length - 1));
                    });
                }
            }
            status.wrongKeystrokeCount = status.keystrokeCount - status.correctKeystrokeCount;
        }
        private volatile bool testRunning = false;
        private bool isKeyBackspace = false;
        private void editText_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            isKeyBackspace = e.Key == VirtualKey.Back;
            switch (e.Key)
            {
                case VirtualKey.Shift:
                case VirtualKey.Control:
                case VirtualKey.CapitalLock:
                    break;
                default:
                    try
                    {
                        if (!testRunning)
                        {
                            testRunning = true;
                            totalKeystrokes = 0;
                            startTimer();
                        }
                        totalKeystrokes++;
                        updateTestBlock();
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine("updateStats Exception : {0}\n{1}", ex.Message, ex.StackTrace);
                    }
                    break;
            }
            Debug.WriteLine("{0} milliseconds to update UI", stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
        }
        private CancellationTokenSource cts;
        private Thread timerThread = null;
        private int timerDuration = 60000;
        private void startTimer()
        {
            Debug.WriteLine("Starting timer");
            textContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(127, 0, 255, 255));
            textContainer.BorderThickness = new Thickness(2.5);
            if (timerThread != null)
            {
                if(cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    cts = null;
                }
                if (timerThread.IsAlive) timerThread.Join();
            }
            cts = new CancellationTokenSource();
            timerThread = new Thread(new ParameterizedThreadStart(async token =>
            {
                var cancellationToken = (CancellationToken)token;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                bool timeout = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    int elapsed = (int)stopWatch.ElapsedMilliseconds;
                    if (elapsed >= timerDuration)
                    {
                        stopWatch.Stop();
                        elapsed = timerDuration;
                        timeout = true;
                    }
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        updateStats(elapsed);
                        if (timeout)
                        {
                            textContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(127, 255, 255, 255));
                            textContainer.BorderThickness = new Thickness(1);
                        }
                    });
                    if (timeout) break;
                    Thread.Sleep(100);
                }

            }));
            timerThread.Start(cts.Token);
        }        
        private void updateStats(int millisecondsElapsed)
        {
            try
            {
                if (millisecondsElapsed == 0 || totalKeystrokes == 0)
                {
                    resetStats();
                    return;
                }
                wpmView.Text = Math.Round((double)status.correctKeystrokeCount * 60000 / millisecondsElapsed / 5).ToString();
                wordCountView.Text = status.wordCount.ToString();
                correctWordCountView.Text = status.correctWordCount.ToString();
                wrongWordCountView.Text = status.wrongWordCount.ToString();
                keystrokeView.Text = status.keystrokeCount.ToString();
                keystrokeCorrectView.Text = status.correctKeystrokeCount.ToString();
                keystrokeWrongView.Text = status.wrongKeystrokeCount.ToString();
                accuracyView.Text = (Math.Round(status.correctKeystrokeCount * 10000.0 / status.keystrokeCount) / 100).ToString() + "%";
                timerView.Text = getTimerString(timerDuration - millisecondsElapsed);
            }catch(Exception e)
            {
                Debug.WriteLine("updateStats Exception : {0}\n{1}", e.Message, e.StackTrace);
            }
        }
        private void resetStats()
        {
            wpmView.Text = wordCountView.Text = correctWordCountView.Text = wrongWordCountView.Text
                = keystrokeView.Text = keystrokeCorrectView.Text = keystrokeWrongView.Text = "0";
            accuracyView.Text = "0.00%";
            timerView.Text = "00:00:000";
        }
        private String getTimerString(int elapsedMilliseconds)
        {
            int milliseconds = elapsedMilliseconds % 1000;
            int seconds = (elapsedMilliseconds / 1000) % 60;
            int minutes = (elapsedMilliseconds / 1000) / 60;
            return String.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        }
        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            totalKeystrokes = 0;
            testRunning = false;
            editText.Text = "";
            generateRandomTestText(1000).ContinueWith(async task =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {
                    testView.updateWords(testWords);
                    updateTestBlock();
                    status.reset();
                    isTyping = false;
                    editText.Focus(FocusState.Keyboard);
                });
            });
        }
    }
}
