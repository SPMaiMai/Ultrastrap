import SwiftUI

struct InstancesView: View {
    @EnvironmentObject private var state: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Running instances")
                .font(.largeTitle.weight(.semibold))
            Text("macOS does not use the Windows Roblox mutexes. Ultrastrap starts extra clients by launching RobloxPlayer directly.")
                .foregroundStyle(Brand.secondaryText)

            HStack {
                Button("Refresh") { state.instances.refresh() }
                Button("Tile grid") { WindowTiler.tileRunningRoblox(mode: .grid) }
                Button("Tile horizontal") { WindowTiler.tileRunningRoblox(mode: .horizontal) }
                Button("Tile vertical") { WindowTiler.tileRunningRoblox(mode: .vertical) }
                Spacer()
                Button("Close all", role: .destructive) {
                    state.instances.terminateAll()
                    state.statusMessage = "Asked every Roblox process to quit."
                }
                .disabled(state.instances.instances.isEmpty)
            }

            if state.instances.instances.isEmpty {
                SectionCard(title: "None running") {
                    Text("No Roblox Player or Studio processes were found.")
                        .foregroundStyle(Brand.secondaryText)
                }
            } else {
                List(state.instances.instances) { instance in
                    HStack {
                        VStack(alignment: .leading) {
                            Text("\(instance.name) · \(instance.kindLabel)")
                            Text("PID \(instance.processId)")
                                .font(.caption)
                                .foregroundStyle(Brand.secondaryText)
                        }
                        Spacer()
                        Button("Focus") { state.instances.focus(instance) }
                        Button("Close", role: .destructive) { state.instances.terminate(instance) }
                    }
                }
                .listStyle(.inset)
            }

            StatusBar(text: state.statusMessage)
            Spacer()
        }
        .onAppear { state.instances.refresh() }
    }
}
