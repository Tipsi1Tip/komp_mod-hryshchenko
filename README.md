# TvLine - імітаційна модель лінії перевірки телевізорів

## Запуск (C#, потрібен .NET 8 SDK)
    dotnet run -- all results       # усі експерименти, CSV у папку results
    dotnet run -- protocol results  # лише протокол верифікації
Режими: all, protocol, transient, duration, pilot.

## Графіки (Python 3, matplotlib)
    python plots.py     # читає results/*.csv, зберігає в fig/
    python schemes.py   # схеми мережі
Папка fig/ має існувати: mkdir fig
