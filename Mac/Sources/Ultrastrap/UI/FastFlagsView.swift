import SwiftUI

struct FastFlagsView: View {
    @EnvironmentObject private var state: AppState
    @State private var fpsCap = "240"
    @State private var customKey = ""
    @State private var customValue = ""
    @State private var jsonText = ""

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("Fast Flags")
                    .font(.largeTitle.weight(.semibold))
                Text("Flags are saved in Ultrastrap, then copied into Roblox.app on every launch. Roblox updates wipe the copy inside the app bundle, so Ultrastrap writes it again next time.")
                    .foregroundStyle(Brand.secondaryText)

                Toggle("Apply Fast Flags when launching Roblox", isOn: $state.settings.useFastFlagManager)
                    .onChange(of: state.settings.useFastFlagManager) { _ in
                        state.saveSettings()
                    }

                SectionCard(title: "Presets") {
                    HStack {
                        TextField("FPS cap", text: $fpsCap)
                            .frame(width: 80)
                        Button("Unlock FPS") {
                            state.flags.setPreset("Rendering.TargetFps", fpsCap)
                            persist()
                        }
                        Button("Clear FPS cap") {
                            state.flags.setPreset("Rendering.TargetFps", nil)
                            persist()
                        }
                    }
                    Toggle("Disable DPI scaling", isOn: disableScalingBinding)
                    Picker("MSAA", selection: msaaBinding) {
                        Text("Default").tag("")
                        Text("1x").tag("1")
                        Text("2x").tag("2")
                        Text("4x").tag("4")
                    }
                    Picker("Texture quality", selection: textureBinding) {
                        Text("Default").tag("")
                        Text("Level 0").tag("0")
                        Text("Level 1").tag("1")
                        Text("Level 2").tag("2")
                        Text("Level 3").tag("3")
                    }
                }

                SectionCard(title: "Custom flag") {
                    HStack {
                        TextField("Flag name", text: $customKey)
                        TextField("Value", text: $customValue)
                        Button("Set") {
                            let key = customKey.trimmingCharacters(in: .whitespacesAndNewlines)
                            guard !key.isEmpty else { return }
                            state.flags.set(key, customValue)
                            persist()
                            customKey = ""
                            customValue = ""
                        }
                    }
                }

                SectionCard(title: "Current flags") {
                    if state.flags.flags.isEmpty {
                        Text("No flags set.")
                            .foregroundStyle(Brand.secondaryText)
                    } else {
                        ForEach(state.flags.flags.keys.sorted(), id: \.self) { key in
                            HStack {
                                Text(key)
                                Spacer()
                                Text(state.flags.flags[key] ?? "")
                                    .foregroundStyle(Brand.secondaryText)
                                Button("Remove", role: .destructive) {
                                    state.flags.set(key, nil)
                                    persist()
                                }
                            }
                        }
                    }
                    HStack {
                        Button("Apply to Roblox.app now") {
                            if let error = state.flags.applyToRoblox() {
                                state.statusMessage = error
                            } else {
                                state.statusMessage = "Wrote Fast Flags into Roblox."
                            }
                        }
                        Button("Reset all", role: .destructive) {
                            state.flags.flags = [:]
                            persist()
                        }
                    }
                }

                SectionCard(title: "Raw JSON") {
                    TextEditor(text: $jsonText)
                        .font(.system(.body, design: .monospaced))
                        .frame(minHeight: 120)
                    Button("Load JSON") { loadJSON() }
                }

                StatusBar(text: state.statusMessage)
            }
        }
        .onAppear {
            jsonText = prettyJSON()
            if let current = state.flags.getPreset("Rendering.TargetFps") {
                fpsCap = current
            }
        }
    }

    private var disableScalingBinding: Binding<Bool> {
        Binding(
            get: { state.flags.getPreset("Rendering.DisableScaling") == "True" },
            set: { value in
                state.flags.setPreset("Rendering.DisableScaling", value ? "True" : nil)
                persist()
            }
        )
    }

    private var msaaBinding: Binding<String> {
        Binding(
            get: { state.flags.getPreset("Rendering.MSAA") ?? "" },
            set: { value in
                state.flags.setPreset("Rendering.MSAA", value.isEmpty ? nil : value)
                persist()
            }
        )
    }

    private var textureBinding: Binding<String> {
        Binding(
            get: { state.flags.getPreset("Rendering.TextureQuality.Level") ?? "" },
            set: { value in
                if value.isEmpty {
                    state.flags.setPreset("Rendering.TextureQuality.OverrideEnabled", nil)
                    state.flags.setPreset("Rendering.TextureQuality.Level", nil)
                } else {
                    state.flags.setPreset("Rendering.TextureQuality.OverrideEnabled", "True")
                    state.flags.setPreset("Rendering.TextureQuality.Level", value)
                }
                persist()
            }
        )
    }

    private func persist() {
        state.flags.save()
        jsonText = prettyJSON()
        state.statusMessage = "Fast Flags saved."
    }

    private func prettyJSON() -> String {
        guard let data = try? JSONSerialization.data(withJSONObject: state.flags.flags, options: [.prettyPrinted, .sortedKeys]),
              let text = String(data: data, encoding: .utf8)
        else {
            return "{}"
        }
        return text
    }

    private func loadJSON() {
        guard let data = jsonText.data(using: .utf8),
              let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
        else {
            state.statusMessage = "That JSON is not a valid object."
            return
        }
        var flags: [String: String] = [:]
        for (key, value) in object {
            flags[key] = String(describing: value)
        }
        state.flags.flags = flags
        persist()
        state.statusMessage = "Loaded Fast Flags from JSON."
    }
}
