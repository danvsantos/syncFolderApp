using System.Globalization;

namespace syncFolder.Strings;

/// <summary>
/// Provides localized strings for English and Portuguese (Brazil).
/// </summary>
public static class Resources
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["syncFolder"] = "syncFolder",
            ["Syncing"] = "Sincronizando",
            ["{0} active"] = "{0} ativo(s)",
            ["{0} error(s)"] = "{0} erro(s)",
            ["No pairs"] = "Nenhum par",
            ["No sync pairs configured"] = "Nenhum par de sincroniza\u00e7\u00e3o configurado",
            ["Open Preferences"] = "Abrir Prefer\u00eancias",
            ["Sync All Now"] = "Sincronizar Tudo Agora",
            ["Preferences..."] = "Prefer\u00eancias...",
            ["Quit syncFolder"] = "Encerrar syncFolder",
            ["Syncing..."] = "Sincronizando...",
            ["Error"] = "Erro",
            ["Waiting for first sync"] = "Aguardando primeira sincroniza\u00e7\u00e3o",
            ["Disabled"] = "Desativado",
            ["OFF"] = "OFF",
            ["Sync Pairs"] = "Pares de Sincroniza\u00e7\u00e3o",
            ["Activity"] = "Atividade",
            ["General"] = "Geral",
            ["About"] = "Sobre",
            ["No Sync Pairs"] = "Nenhum Par de Sincroniza\u00e7\u00e3o",
            ["Add a sync pair to start synchronizing folders automatically."] = "Adicione um par de sincroniza\u00e7\u00e3o para come\u00e7ar a sincronizar pastas automaticamente.",
            ["Add Sync Pair"] = "Adicionar Par de Sincroniza\u00e7\u00e3o",
            ["Sync All"] = "Sincronizar Tudo",
            ["No Activity Yet"] = "Nenhuma Atividade Ainda",
            ["Sync activity will appear here."] = "A atividade de sincroniza\u00e7\u00e3o aparecer\u00e1 aqui.",
            ["{0} entries"] = "{0} entradas",
            ["Clear Log"] = "Limpar Log",
            ["Launch syncFolder at Login"] = "Iniciar syncFolder no Login",
            ["When enabled, syncFolder will start automatically when you log in."] = "Quando ativado, o syncFolder iniciar\u00e1 automaticamente ao fazer login.",
            ["Enable sync notifications"] = "Ativar notifica\u00e7\u00f5es de sincroniza\u00e7\u00e3o",
            ["Show notifications when syncs complete with changes or errors."] = "Mostrar notifica\u00e7\u00f5es quando sincroniza\u00e7\u00f5es completarem com altera\u00e7\u00f5es ou erros.",
            ["Author: Daniel Vieira"] = "Autor: Daniel Vieira",
            ["GitHub Repository"] = "Reposit\u00f3rio GitHub",
            ["Licensed under the MIT License"] = "Licenciado sob a Licen\u00e7a MIT",
            ["Version 1.1.0"] = "Vers\u00e3o 1.1.0",
            ["Automatic folder synchronization for Windows.\nKeep your folders in sync with configurable polling intervals."] = "Sincroniza\u00e7\u00e3o autom\u00e1tica de pastas para Windows.\nMantenha suas pastas sincronizadas com intervalos de verifica\u00e7\u00e3o configur\u00e1veis.",
            ["Edit Sync Pair"] = "Editar Par de Sincroniza\u00e7\u00e3o",
            ["Name:"] = "Nome:",
            ["Source:"] = "Origem:",
            ["Destination:"] = "Destino:",
            ["Browse..."] = "Procurar...",
            ["Interval:"] = "Intervalo:",
            ["1 minute"] = "1 minuto",
            ["2 minutes"] = "2 minutos",
            ["5 minutes"] = "5 minutos",
            ["10 minutes"] = "10 minutos",
            ["15 minutes"] = "15 minutos",
            ["30 minutes"] = "30 minutos",
            ["60 minutes"] = "60 minutos",
            ["120 minutes"] = "120 minutos",
            ["Sync Mode:"] = "Modo de Sincroniza\u00e7\u00e3o:",
            ["One-way (Source \u2192 Destination)"] = "Unidirecional (Origem \u2192 Destino)",
            ["Bidirectional"] = "Bidirecional",
            ["Conflicts resolved by keeping the most recently modified file."] = "Conflitos resolvidos mantendo o arquivo modificado mais recentemente.",
            ["Delete orphans in destination"] = "Excluir arquivos \u00f3rf\u00e3os no destino",
            ["Enabled"] = "Ativado",
            ["Exclude Filters"] = "Filtros de Exclus\u00e3o",
            ["Add"] = "Adicionar",
            ["Presets:"] = "Predefinidos:",
            ["Source and destination must be different."] = "Origem e destino devem ser diferentes.",
            ["Cancel"] = "Cancelar",
            ["Save"] = "Salvar",
            ["DISABLED"] = "DESATIVADO",
            ["Every {0} min"] = "A cada {0} min",
            ["Delete orphans"] = "Excluir \u00f3rf\u00e3os",
            ["Sync now"] = "Sincronizar agora",
            ["Sync Now"] = "Sincronizar Agora",
            ["{0} copied"] = "{0} copiado(s)",
            ["{0} deleted"] = "{0} exclu\u00eddo(s)",
            ["No changes"] = "Sem altera\u00e7\u00f5es",
            ["Edit..."] = "Editar...",
            ["Delete"] = "Excluir",
            ["Delete Sync Pair?"] = "Excluir Par de Sincroniza\u00e7\u00e3o?",
            ["Are you sure you want to remove \"{0}\"? Files in the destination will not be deleted."] = "Tem certeza que deseja remover \"{0}\"? Os arquivos no destino n\u00e3o ser\u00e3o exclu\u00eddos.",
            ["Add sync pair"] = "Adicionar par de sincroniza\u00e7\u00e3o",
            ["syncFolder Preferences"] = "Prefer\u00eancias do syncFolder",
            ["My Documents Backup"] = "Backup de Documentos",
        }
    };

    private static bool IsPtBr =>
        CultureInfo.CurrentUICulture.Name.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

    public static string Get(string key)
    {
        if (IsPtBr && Translations.TryGetValue("pt-BR", out var ptDict) && ptDict.TryGetValue(key, out var ptValue))
            return ptValue;
        return key;
    }

    public static string Get(string key, params object[] args)
    {
        var template = Get(key);
        return string.Format(template, args);
    }

    // Convenience properties for frequently used strings
    public static string SyncFolder => Get("syncFolder");
    public static string Syncing => Get("Syncing");
    public static string SyncingDots => Get("Syncing...");
    public static string Error => Get("Error");
    public static string NoPairs => Get("No pairs");
    public static string NoSyncPairsConfigured => Get("No sync pairs configured");
    public static string OpenPreferences => Get("Open Preferences");
    public static string SyncAllNow => Get("Sync All Now");
    public static string Preferences => Get("Preferences...");
    public static string QuitSyncFolder => Get("Quit syncFolder");
    public static string WaitingForFirstSync => Get("Waiting for first sync");
    public static string Disabled => Get("Disabled");
    public static string Off => Get("OFF");
    public static string SyncPairs => Get("Sync Pairs");
    public static string Activity => Get("Activity");
    public static string General => Get("General");
    public static string About => Get("About");
    public static string NoSyncPairs => Get("No Sync Pairs");
    public static string AddSyncPairDescription => Get("Add a sync pair to start synchronizing folders automatically.");
    public static string AddSyncPair => Get("Add Sync Pair");
    public static string SyncAll => Get("Sync All");
    public static string NoActivityYet => Get("No Activity Yet");
    public static string SyncActivityWillAppear => Get("Sync activity will appear here.");
    public static string ClearLog => Get("Clear Log");
    public static string LaunchAtLogin => Get("Launch syncFolder at Login");
    public static string LaunchAtLoginDescription => Get("When enabled, syncFolder will start automatically when you log in.");
    public static string EnableNotifications => Get("Enable sync notifications");
    public static string NotificationsDescription => Get("Show notifications when syncs complete with changes or errors.");
    public static string AuthorDaniel => Get("Author: Daniel Vieira");
    public static string GitHubRepository => Get("GitHub Repository");
    public static string MITLicense => Get("Licensed under the MIT License");
    public static string Version => Get("Version 1.1.0");
    public static string AppDescription => Get("Automatic folder synchronization for Windows.\nKeep your folders in sync with configurable polling intervals.");
    public static string EditSyncPair => Get("Edit Sync Pair");
    public static string NameLabel => Get("Name:");
    public static string SourceLabel => Get("Source:");
    public static string DestinationLabel => Get("Destination:");
    public static string Browse => Get("Browse...");
    public static string IntervalLabel => Get("Interval:");
    public static string SyncModeLabel => Get("Sync Mode:");
    public static string OneWay => Get("One-way (Source \u2192 Destination)");
    public static string Bidirectional => Get("Bidirectional");
    public static string ConflictDescription => Get("Conflicts resolved by keeping the most recently modified file.");
    public static string DeleteOrphans => Get("Delete orphans in destination");
    public static string Enabled => Get("Enabled");
    public static string ExcludeFilters => Get("Exclude Filters");
    public static string Add => Get("Add");
    public static string Presets => Get("Presets:");
    public static string PathsMustDiffer => Get("Source and destination must be different.");
    public static string Cancel => Get("Cancel");
    public static string Save => Get("Save");
    public static string DeleteLabel => Get("Delete");
    public static string DeleteSyncPairQuestion => Get("Delete Sync Pair?");
    public static string SyncNow => Get("Sync Now");
    public static string EditDots => Get("Edit...");
    public static string SyncFolderPreferences => Get("syncFolder Preferences");
    public static string NoChanges => Get("No changes");
    public static string MyDocumentsBackup => Get("My Documents Backup");
}
