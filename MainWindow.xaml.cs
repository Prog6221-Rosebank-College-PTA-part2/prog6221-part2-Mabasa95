using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Part_2
{
    public partial class MainWindow : Window
    {
        private string userName = "";
        private readonly Random random = new Random();
        private readonly string memoryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory.txt");
        private string currentTopic = "";

        // Helper objects
        private readonly DatabaseHelper dbHelper = new DatabaseHelper();
        private List<QuizQuestion> quizQuestions = new List<QuizQuestion>();
        private int currentQuestionIndex = 0;
        private int quizScore = 0;

        // Managers
        private readonly ActivityLogManager logManager;
        private readonly TaskManager taskManager;

        // Chatbot Responses
        private readonly Dictionary<string, string[]> cyberResponses = new Dictionary<string, string[]>()
        {
            {
                "phishing",
                new string[]
                {
                    "Phishing is a cyberattack in which an attacker impersonates a trusted person or organization to trick individuals into revealing sensitive information.",
                    "Social engineering often uses fraudulent emails, messages, or websites to deceive users.",
                    "An unauthorized attempt to gain access to accounts by exploiting trust through deceptive communications."
                }
            },
            {
                "malware",
                new string[]
                {
                    "Malware is software created to perform malicious actions on a computer system without the user's consent.",
                    "A broad category of programs (viruses, spyware, ransomware) used to compromise digital systems.",
                    "Executable code that exploits vulnerabilities, steals data, or disrupts operations."
                }
            },
            {
                "password",
                new string[]
                {
                    "A password is a confidential sequence of characters used to authenticate a user's identity.",
                    "Use a long, unique passphrase containing letters, numbers, and symbols to protect accounts.",
                    "Consider using a password manager instead of writing credentials down."
                }
            },
            {
                "scam",
                new string[]
                {
                    "A scam is a fraudulent scheme intended to trick people for financial or personal gain.",
                    "Deceptive practices persuade victims to provide money, sensitive information, or remote access under false pretenses.",
                    "Common examples include tech support scams, phishing emails, and investment scams."
                }
            },
            {
                "privacy",
                new string[]
                {
                    "Privacy is the state of being free from unwanted observation, intrusion, or interference.",
                    "Review and restrict sharing settings on social networks to safeguard personal data.",
                    "Having control over your personal space, information, and digital footprint."
                }
            }
        };

        private readonly Dictionary<string, string[]> topicKeyWord = new Dictionary<string, string[]>()
        {
            { "phishing", new string[]{ "fraudulent emails", "suspicious messages", "harmful links", "phishing" } },
            { "malware", new string[]{ "virus", "spyware", "ransomware", "trojan horse", "worm" } },
            { "password", new string[]{ "graphical password", "otp", "passphrase", "password" } },
            { "scam", new string[]{ "online scams", "impersonation", "investment scams", "scam" } },
            { "privacy", new string[]{ "privacy setting", "personal data", "privacies", "privacy" } }
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeQuizQuestions();

            // Initialize structural managers
            logManager = new ActivityLogManager(dbHelper);
            taskManager = new TaskManager(dbHelper, logManager);
        }

        // Start button
        public void Start_Click(object sender, RoutedEventArgs e)
        {
            WelcomeGrid.Visibility = Visibility.Collapsed;
            NameGrid.Visibility = Visibility.Visible;

            try
            {
                Greeting greeting = new Greeting();
                greeting.PlayVoiceGreeting();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio failed: {ex.Message}");
            }
        }

        // Submit name
        public void Submit_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Name cannot be empty";
                MessageBox.Show("Please enter a username to proceed.", "Validation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z\s]+$"))
            {
                ErrorText.Text = "Enter a valid name";
                MessageBox.Show("Please enter a valid alphabetic name.", "Validation Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            userName = name;
            ErrorText.Text = "";

            // Propagate username to managers
            logManager.UserName = userName;
            taskManager.UserName = userName;

            NameGrid.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Visible; // Show main screen

            LogoText.Text = AsciiArt.ShieldLogo;

            // Load saved topic
            Dictionary<string, string> userMemory = LoadUserMemory();
            if (userMemory.ContainsKey(userName))
            {
                string savedTopic = userMemory[userName];
                ChatListBox.AppendText($"Chatbot: Welcome back, {userName}! I remember your favorite cybersecurity topic is: {savedTopic}.\n\n");
            }
            else
            {
                ChatListBox.AppendText($"Chatbot: Welcome to AI Cybersecurity Assistant, {userName}!\nWhat is your favorite cybersecurity topic? I'D love to know!\n\n");
            }

            // Load database 
            TryDatabaseAction(() => {
                dbHelper.InitializeDatabase();
                logManager.Log("User logged in and initialized session");
                RefreshTasks();
                RefreshLogs();
            }, "Initialize database connection");
        }

        // Handle database errors
        private void TryDatabaseAction(Action action, string actionDescription)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to complete the database action({actionDescription}). Please make sure your MySQL server is running, then try again.\n\nDetails: {ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Chat functions
        public void Send_Click(object sender, RoutedEventArgs e)
        {
            string rawMessage = InputTextBox.Text;
            string message = rawMessage.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(message))
            {
                ChatListBox.AppendText("Chatbot: I’m not sure I understand that. Could you rephrase your question?\n");
                InputTextBox.Clear();
                return;
            }

            ChatListBox.AppendText($"{userName}: {rawMessage}\n");

            // Check for log request
            if (message.Contains("activity log") || message.Contains("what have you done for me"))
            {
                TryDatabaseAction(() => {
                    var logs = logManager.GetLogs();
                    ChatListBox.AppendText("Chatbot: Here is a summary of your recent activity:\n");
                    for (int i = 0; i < logs.Count; i++)
                    {
                        ChatListBox.AppendText($" {i + 1}. {logs[i]}\n");
                    }
                    ChatListBox.AppendText("\n");
                    logManager.Log("Requested activity log summary via chatbot");
                }, "Fetch logs via chat");

                InputTextBox.Clear();
                RefreshLogs();
                return;
            }

            // Check for a new task
            if (message.StartsWith("add task") || message.StartsWith("remind me to"))
            {
                ParseAndAddTaskFromChat(message);
                InputTextBox.Clear();
                return;
            }

            // Check for quiz
            if (message.Contains("start quiz") || message.Contains("play quiz") || message.Contains("quiz"))
            {
                MainTabControl.SelectedIndex = 2; // Switch to Quiz tab
                ChatListBox.AppendText("Chatbot: Let's test your cybersecurity knowledge! I've opened the Quiz tab for you.\n\n");
                InputTextBox.Clear();
                return;
            }

            // Save favorite topic
            if (message.Contains("interested in") || message.Contains("favorite topic is"))
            {
                SaveToFile(message);
                InputTextBox.Clear();
                return;
            }

            string botResponse = chatBotResponse(message);
            ChatListBox.AppendText($"Chatbot: {botResponse} \n\n");
            InputTextBox.Clear();
        }

        // Add task from chat
        private void ParseAndAddTaskFromChat(string text)
        {
            TryDatabaseAction(() => {
                bool success = taskManager.ParseAndAddTaskFromChat(text, out string resultMessage);
                ChatListBox.AppendText($"Chatbot: {resultMessage}\n\n");
                if (success)
                {
                    RefreshTasks();
                    RefreshLogs();
                }
            }, "Add task from Chatbot NLP");
        }

        public string chatBotResponse(string message)
        {
            string sentiment = DetectSentiment(message);
            bool moreInfo = isFollowUp(message);
            string topic = DetectTopic(message);

            if (string.IsNullOrEmpty(topic) && moreInfo && !string.IsNullOrEmpty(currentTopic))
            {
                topic = currentTopic;
            }

            if (!string.IsNullOrEmpty(topic))
            {
                currentTopic = topic;
                return BuildResponses(topic, sentiment, moreInfo);
            }

            if (!string.IsNullOrEmpty(sentiment))
            {
                return $"{GetSentimentSupport(sentiment)} Tell me which cybersecurity topic you'd like to learn about(phishing, malware, scams, passwords, or privacy) and I'll be happy to help.";
            }

            return "I didn’t quite understand that. Can you try rephrasing? Try asking about phishing or password safety.";
        }

        public string DetectTopic(string message)
        {
            foreach (var topic in topicKeyWord)
            {
                if (topic.Value.Any(word => message.Contains(word)))
                    return topic.Key;
            }
            foreach (var topic in cyberResponses)
            {
                if (message.Contains(topic.Key))
                    return topic.Key;
            }
            return "";
        }

        public string BuildResponses(string topic, string sentiment, bool moreInfo)
        {
            if (!cyberResponses.ContainsKey(topic))
                return "I can offer general cybersecurity advice, but I don't have specific information on that topic yet.";

            string[] foundResponse = cyberResponses[topic];
            int index = random.Next(foundResponse.Length);
            string response = foundResponse[index];
            string support = GetSentimentSupport(sentiment);

            if (!string.IsNullOrEmpty(support))
                return $"{support}\nHere's some helpful information:\n-> {response}";

            return response;
        }

        public string GetSentimentSupport(string sentiment)
        {
            if (sentiment == "worried")
                return $"Hey {userName}, it's completely understandable to feel worried. Digital threats are common, but healthy security habits protect you.";
            if (sentiment == "frustrated")
                return $"Hey {userName}, I know this is frustrating. Let's tackle this step-by-step.";
            if (sentiment == "curious")
                return $"It's great that you are curious about digital safety, {userName}! Proactive learning is excellent defense.";

            return "";
        }

        public string DetectSentiment(string message)
        {
            if (message.Contains("worried") || message.Contains("anxious") || message.Contains("nervous") || message.Contains("unsure") || message.Contains("afraid"))
                return "worried";
            if (message.Contains("frustrated") || message.Contains("annoyed") || message.Contains("angry") || message.Contains("confused") || message.Contains("stuck"))
                return "frustrated";
            if (message.Contains("curious") || message.Contains("interested") || message.Contains("learn") || message.Contains("wondering"))
                return "curious";

            return "";
        }

        public bool isFollowUp(string message)
        {
            return message.Contains("explain more") || message.Contains("more details") || message.Contains("another tip") || message.Contains("tell me more");
        }

        // Task methods 
        private void RefreshTasks()
        {
            TaskListView.ItemsSource = taskManager.GetTasks();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text.Trim();
            string desc = TaskDescTextBox.Text.Trim();
            DateTime? date = TaskDatePicker.SelectedDate;

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a title for your task.", "Form Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TryDatabaseAction(() => {
                taskManager.AddTask(title, desc, date);

                // Clear the form
                TaskTitleTextBox.Clear();
                TaskDescTextBox.Clear();
                TaskDatePicker.SelectedDate = null;

                RefreshTasks();
                RefreshLogs();
                MessageBox.Show("Your task has been added successfully.", "Database Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }, "Add Task");
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is TaskItem selectedTask)
            {
                TryDatabaseAction(() => {
                    taskManager.CompleteTask(selectedTask.Id, selectedTask.Title);
                    RefreshTasks();
                    RefreshLogs();
                }, "Mark Task Completed");
            }
            else
            {
                MessageBox.Show("Please select a task from the list before continuing.", "Selection Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is TaskItem selectedTask)
            {
                var result = MessageBox.Show($"Are you sure you want to delete task '{selectedTask.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    TryDatabaseAction(() => {
                        taskManager.DeleteTask(selectedTask.Id, selectedTask.Title);
                        RefreshTasks();
                        RefreshLogs();
                    }, "Delete Task");
                }
            }
            else
            {
                MessageBox.Show("Please select a task you would like to delete.", "Selection Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Quiz methods
        private void InitializeQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>()
            {
                new QuizQuestion("Which of the following is a sign of a phishing email?", "Generic greeting & urgent threat", "Official company email address", "Correct spelling & grammar", "No links in body", "A"),
                new QuizQuestion("Is it safe to write down your passwords on a post-it note on your monitor?", "True", "False", "", "", "B"),
                new QuizQuestion("What is malware?", "Harmful software designed to exploit systems", "Antivirus software", "A hardware component", "Safe web browsing tools", "A"),
                new QuizQuestion("Two-Factor Authentication (2FA) significantly secures your accounts.", "True", "False", "", "", "A"),
                new QuizQuestion("Which protocols verify that website data is encrypted during transmission?", "HTTPS", "HTTP", "FTP", "SMTP", "A"),
                new QuizQuestion("Social engineering relies on manipulating human psychology rather than technical exploits.", "True", "False", "", "", "A"),
                new QuizQuestion("Ransomware is a type of malware that locks down personal files and demands payment.", "True", "False", "", "", "A"),
                new QuizQuestion("It is safe to use public open Wi-Fi for bank transactions if you do not have cellular data.", "True", "False", "", "", "B"),
                new QuizQuestion("What should you do if you receive an email claiming you won a lottery from an unknown address?", "Delete/Report it as spam", "Click the links immediately", "Provide personal information", "Reply to ask for details", "A"),
                new QuizQuestion("A secure password should contain at least 12 characters, including mix of cases, numbers, and symbols.", "True", "False", "", "", "A"),
                new QuizQuestion("Antivirus software must be updated constantly to protect against newly emerged security signatures.", "True", "False", "", "", "A")
            };
        }

        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            QuizStartPanel.Visibility = Visibility.Collapsed;
            QuizQuestionPanel.Visibility = Visibility.Visible;
            QuizScorePanel.Visibility = Visibility.Collapsed;

            currentQuestionIndex = 0;
            quizScore = 0;

            TryDatabaseAction(() => {
                logManager.Log("Started Cybersecurity Awareness Quiz");
                RefreshLogs();
            }, "Log quiz start");

            ShowQuestion();
        }

        private void ShowQuestion()
        {
            QuizFeedbackText.Visibility = Visibility.Collapsed;
            SubmitAnswerButton.Visibility = Visibility.Visible;
            NextQuestionButton.Visibility = Visibility.Collapsed;

            // Clear answers
            OptARadioButton.IsChecked = false;
            OptBRadioButton.IsChecked = false;
            OptCRadioButton.IsChecked = false;
            OptDRadioButton.IsChecked = false;

            var currentQ = quizQuestions[currentQuestionIndex];
            QuizCounterText.Text = $"Question {currentQuestionIndex + 1} of {quizQuestions.Count}";
            QuestionTextBlock.Text = currentQ.QuestionText;

            // Show answer options
            OptARadioButton.Content = currentQ.OptA;
            OptBRadioButton.Content = currentQ.OptB;

            if (string.IsNullOrEmpty(currentQ.OptC))
            {
                OptCRadioButton.Visibility = Visibility.Collapsed;
                OptDRadioButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                OptCRadioButton.Visibility = Visibility.Visible;
                OptDRadioButton.Visibility = Visibility.Visible;
                OptCRadioButton.Content = currentQ.OptC;
                OptDRadioButton.Content = currentQ.OptD;
            }
        }

        private void SubmitAnswer_Click(object sender, RoutedEventArgs e)
        {
            string selected = "";
            if (OptARadioButton.IsChecked == true) selected = "A";
            else if (OptBRadioButton.IsChecked == true) selected = "B";
            else if (OptCRadioButton.IsChecked == true) selected = "C";
            else if (OptDRadioButton.IsChecked == true) selected = "D";

            if (string.IsNullOrEmpty(selected))
            {
                MessageBox.Show("Please select an answer before submitting.", "Quiz Action Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentQ = quizQuestions[currentQuestionIndex];
            bool isCorrect = (selected == currentQ.CorrectAnswer);

            if (isCorrect)
            {
                quizScore++;
                QuizFeedbackText.Text = "Correct! Excellent work, you understand this cybersecurity concept.";
                QuizFeedbackText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                QuizFeedbackText.Text = $"That's not correct. The correct answer was ({currentQ.CorrectAnswer}). Keep practicing, you'll improve!";
                QuizFeedbackText.Foreground = System.Windows.Media.Brushes.Red;
            }

            QuizFeedbackText.Visibility = Visibility.Visible;
            SubmitAnswerButton.Visibility = Visibility.Collapsed;
            NextQuestionButton.Visibility = Visibility.Visible;
        }

        private void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < quizQuestions.Count)
            {
                ShowQuestion();
            }
            else
            {
                // Finish quiz
                QuizQuestionPanel.Visibility = Visibility.Collapsed;
                QuizScorePanel.Visibility = Visibility.Visible;

                QuizFinalScoreText.Text = $"Final Score: {quizScore} / {quizQuestions.Count}";

                if (quizScore == quizQuestions.Count)
                    QuizConclusionText.Text = "Outstanding! You have excellent cybersecurity knowledge!";
                else if (quizScore >= 7)
                    QuizConclusionText.Text = "Great job! You have a strong understanding of cybersecurity threats.";
                else
                    QuizConclusionText.Text = "Keep learning and practicing to stay safe online. You're making great progress!";

                TryDatabaseAction(() => {
                    logManager.Log($"Completed Quiz. Score: {quizScore}/{quizQuestions.Count}");
                    RefreshLogs();
                }, "Log quiz end");
            }
        }

        private void PlayAgain_Click(object sender, RoutedEventArgs e)
        {
            QuizStartPanel.Visibility = Visibility.Visible;
            QuizScorePanel.Visibility = Visibility.Collapsed;
        }

        // Activity log methods
        private void RefreshLogs()
        {
            TryDatabaseAction(() => {
                LogListBox.ItemsSource = logManager.GetLogs();
            }, "Load recent activity logs");
        }

        private void ShowMoreLogs_Click(object sender, RoutedEventArgs e)
        {
            TryDatabaseAction(() => {
                LogListBox.ItemsSource = logManager.ShowMoreLogs();
            }, "Load more activity logs");
        }

        // Track tab changes
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && !string.IsNullOrEmpty(userName))
            {
                TabItem selectedTab = (TabItem)MainTabControl.SelectedItem;
                if (selectedTab != null)
                {
                    TryDatabaseAction(() => {
                        logManager.Log($"Navigated to tab: {selectedTab.Header}");
                        RefreshLogs();
                    }, "Log tab change navigation");
                }
            }
        }

        // Save user memory
        private Dictionary<string, string> LoadUserMemory()
        {
            var memory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(memoryFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(memoryFile);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            memory[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"File read error: {ioEx.Message}");
                }
            }
            return memory;
        }

        private void SaveUserMemory(string username, string topic)
        {
            Dictionary<string, string> memory = LoadUserMemory();
            memory[username] = topic;

            List<string> lines = new List<string>();
            foreach (var kvp in memory)
            {
                lines.Add($"{kvp.Key}={kvp.Value}");
            }

            try
            {
                File.WriteAllLines(memoryFile, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save favorite topic: {ex.Message}", "File Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveToFile(string message)
        {
            string topic = "";
            string[] rawPrefixes = { "i am interested in", "interested in", "my favorite topic is", "favorite topic is" };

            foreach (var prefix in rawPrefixes)
            {
                if (message.Contains(prefix))
                {
                    int index = message.IndexOf(prefix);
                    topic = message.Substring(index + prefix.Length).Trim();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(topic))
            {
                SaveUserMemory(userName, topic);
                ChatListBox.AppendText($"Chatbot: Thanks! I'll remember that your favorite cybersecurity topic is \"{topic}\"!\n\n");
            }
            else
            {
                ChatListBox.AppendText("Chatbot: I couldn't identify the topic you'd like to save. Please try typing: 'My favorite topic is [topic]'\n\n");
            }
        }
    }

    // Quiz question
    public struct QuizQuestion
    {
        public string QuestionText { get; set; }
        public string OptA { get; set; }
        public string OptB { get; set; }
        public string OptC { get; set; }
        public string OptD { get; set; }
        public string CorrectAnswer { get; set; }

        public QuizQuestion(string text, string a, string b, string c, string d, string correct)
        {
            QuestionText = text;
            OptA = a;
            OptB = b;
            OptC = c;
            OptD = d;
            CorrectAnswer = correct;
        }
    }
}