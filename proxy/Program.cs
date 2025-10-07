using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Win32;
using System.Threading.Tasks;

class ProxyManager
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Настройка системного прокси ===");

        while (true)
        {
            Console.WriteLine("\n1. Установить прокси");
            Console.WriteLine("2. Отключить прокси");
            Console.WriteLine("3. Протестировать прокси");
            Console.WriteLine("4. Проверить текущие настройки");
            Console.WriteLine("5. Выход");

            Console.Write("Выберите действие (1-5): ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await SetProxy();
                    break;
                case "2":
                    DisableProxy();
                    break;
                case "3":
                    await TestProxy();
                    break;
                case "4":
                    CheckCurrentProxy();
                    break;
                case "5":
                    Console.WriteLine("Выход из программы");
                    return;
                default:
                    Console.WriteLine("Неверный выбор");
                    break;
            }
        }
    }

    static async Task SetProxy()
    {
        Console.Write("Введите адрес прокси-сервера (host:port): ");
        string proxyAddress = Console.ReadLine().Trim();

        Console.Write("Введите логин (если требуется, иначе нажмите Enter): ");
        string username = Console.ReadLine();

        Console.Write("Введите пароль (если требуется, иначе нажмите Enter): ");
        string password = Console.ReadLine();

        // Тестируем прокси перед настройкой
        bool isWorking = await TestProxyConnection(proxyAddress, username, password);

        if (isWorking)
        {
            SetSystemProxy(proxyAddress);
            Console.WriteLine("Системный прокси успешно настроен!");
            Console.WriteLine("Перезапустите браузер для применения настроек");
        }
        else
        {
            Console.WriteLine(" Прокси не работает, настройка отменена");
        }
    }

    static async Task<bool> TestProxyConnection(string proxyAddress, string username = null, string password = null)
    {
        Console.WriteLine($"\nТестируем подключение через прокси: {proxyAddress}");

        try
        {
            var httpClientHandler = new HttpClientHandler();

            // Настраиваем прокси
            if (!string.IsNullOrEmpty(proxyAddress))
            {
                var proxy = new WebProxy(proxyAddress);

                // Добавляем учетные данные если есть
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    proxy.Credentials = new NetworkCredential(username, password);
                }

                httpClientHandler.Proxy = proxy;
                httpClientHandler.UseProxy = true;
            }

            using var client = new HttpClient(httpClientHandler);
            client.Timeout = TimeSpan.FromSeconds(15);

            // Пробуем разные сервисы для тестирования
            string[] testUrls = {
                "http://api.ipify.org?format=json",
                "http://httpbin.org/ip",
                "http://ipinfo.io/json"
            };

            foreach (string testUrl in testUrls)
            {
                try
                {
                    Console.WriteLine($"Пробуем: {testUrl}");
                    HttpResponseMessage response = await client.GetAsync(testUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Успешно! Ответ получен");

                        // Пробуем распарсить JSON для получения IP
                        try
                        {
                            using JsonDocument doc = JsonDocument.Parse(content);
                            if (doc.RootElement.TryGetProperty("ip", out JsonElement ipElement))
                            {
                                Console.WriteLine($"Ваш IP через прокси: {ipElement.GetString()}");
                            }
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine($"Получен ответ (не JSON): {content.Substring(0, Math.Min(100, content.Length))}...");
                        }

                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"HTTP ошибка: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
            return false;
        }
    }

    static void SetSystemProxy(string proxyAddress)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);

            if (key != null)
            {
                // Включаем прокси
                key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);

                // Устанавливаем адрес прокси
                key.SetValue("ProxyServer", proxyAddress, RegistryValueKind.String);

                // Отключаем обход для локальных адресов
                key.SetValue("ProxyOverride", "", RegistryValueKind.String);

                Console.WriteLine($"Прокси установлен: {proxyAddress}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка настройки системного прокси: {ex.Message}");
        }
    }

    static void DisableProxy()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);

            if (key != null)
            {
                key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                Console.WriteLine("✓ Системный прокси отключен");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отключения прокси: {ex.Message}");
        }
    }

    static void CheckCurrentProxy()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", false);

            if (key != null)
            {
                object proxyEnable = key.GetValue("ProxyEnable");
                object proxyServer = key.GetValue("ProxyServer");

                if (proxyEnable != null && (int)proxyEnable == 1)
                {
                    Console.WriteLine($"Прокси включен: {proxyServer}");
                }
                else
                {
                    Console.WriteLine("Прокси отключен");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка проверки настроек: {ex.Message}");
        }
    }

    static async Task TestProxy()
    {
        Console.Write("Введите адрес прокси для теста (host:port): ");
        string proxyAddress = Console.ReadLine().Trim();

        Console.Write("Логин (если есть): ");
        string username = Console.ReadLine();

        Console.Write("Пароль (если есть): ");
        string password = Console.ReadLine();

        await TestProxyConnection(proxyAddress, username, password);
    }
}