using System;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Colors;

using ImGuiNET;

using GearsetHelperPlugin;

namespace GearsetHelperPlugin.Ui;

internal class SettingsWindow {

	private readonly PluginUI Ui;

	private bool visible = false;

	private string token = string.Empty;
	private string username = string.Empty;
	private string password = string.Empty;

	private Task<Exporter.EtroLoginResponse>? LoginTask;

	private Configuration Config => Ui.Plugin.Config;

	public bool Visible {
		get => visible;
		set {
			visible = value;
		}
	}

	public SettingsWindow(PluginUI ui) {
		Ui = ui;
	}

	public void OpenSettings() {
		visible = true;
		username = string.Empty;
		password = string.Empty;
		token = Config.EtroApiKey ?? string.Empty;
	}

	public void Draw() {
		if (!Visible)
			return;

		float scale = ImGui.GetFontSize() / 17;

		ImGui.SetNextWindowSize(new Vector2(370 * scale, 100), ImGuiCond.Appearing);
		ImGui.SetNextWindowSizeConstraints(new Vector2(370 * scale, 100), new Vector2(370 * scale, float.MaxValue));

		if (ImGui.Begin($"{Ui.Plugin.Name} Settings", ref visible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)) {
			ImGui.TextColored(ImGuiColors.DalamudGrey, "Position");

			bool attach = Config.AttachToExamine;
			if (ImGui.Checkbox("Attach to Examine", ref attach)) {
				Config.AttachToExamine = attach;
				Config.Save();
			}

			ImGui.Indent();

			int side = Config.AttachSide;
			if (ImGui.Combo("Side", ref side, "Left\x00Right")) {
				Config.AttachSide = side;
				Config.Save();
			}

			ImGui.Unindent();

			ImGui.Spacing();

			ImGui.TextColored(ImGuiColors.DalamudGrey, "Etro Support");
			ImGui.TextWrapped("In order to export gearsets to Etro, you need to authenticate yourself. If you use Discord to login, you'll need to enter an API key manually.");

			if (LoginTask == null) {
				bool logged_in = !string.IsNullOrEmpty(Config.EtroApiKey);

				ImGui.InputText("Key", ref token, 1024, ImGuiInputTextFlags.AutoSelectAll | (logged_in ? (ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.Password) : ImGuiInputTextFlags.None));
				if (!logged_in) {
					if (ImGui.Button("Save")) {
						Config.EtroApiKey = string.IsNullOrEmpty(token) ? null : token;
						Config.EtroRefreshKey = null;
						Config.Save();
						Ui.Plugin.Exporter.ClearError();
					}

					bool login = false;

					if (ImGui.InputText("Username", ref username, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
						login = true;
					if (ImGui.InputText("Password", ref password, 1024, ImGuiInputTextFlags.Password | ImGuiInputTextFlags.EnterReturnsTrue))
						login = true;
					if (ImGui.Button("Login"))
						login = true;

					if (login && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
						LoginTask = Ui.Plugin.Exporter.LoginEtro(username, password);

				} else {
					if (ImGui.Button("Log Out")) {
						token = string.Empty;
						Config.EtroApiKey = null;
						Config.EtroRefreshKey = null;
						Config.Save();
						Ui.Plugin.Exporter.ClearError();
					}
				}

			} else { 
				if (LoginTask.IsCompleted) {
					if (!string.IsNullOrEmpty(LoginTask.Result.ApiKey)) {
						Config.EtroApiKey = LoginTask.Result.ApiKey;
						Config.EtroRefreshKey = LoginTask.Result.RefreshKey;
						Config.Save();
						Ui.Plugin.Exporter.ClearError();

						username = string.Empty;
						password = string.Empty;
						token = Config.EtroApiKey;
						LoginTask = null;

					} else {
						ImGui.TextColored(ImGuiColors.DalamudYellow, "Error");
						ImGui.TextWrapped(LoginTask.Result.Error ?? "An unknown error occurred.");
						if (ImGui.Button("OK"))
							LoginTask = null;
					}
				} else
					ImGui.TextColored(ImGuiColors.ParsedGrey, "Logging in...");
			}
		}
	}

}