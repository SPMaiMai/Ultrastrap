import SwiftUI

struct AboutView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(spacing: 14) {
                Brand.swiftUIIcon
                    .resizable()
                    .interpolation(.high)
                    .frame(width: 72, height: 72)
                VStack(alignment: .leading, spacing: 4) {
                    Text("Ultrastrap")
                        .font(.largeTitle.weight(.semibold))
                    Text("Version \(Brand.version) for macOS")
                        .foregroundStyle(Brand.secondaryText)
                }
            }

            Text("Ultrastrap is a feature-expanded fork of Bloxstrap. This Mac app is a native rewrite — the Windows WPF app cannot run on macOS.")
                .fixedSize(horizontal: false, vertical: true)

            Text("This is an independent fork. It is not affiliated with or endorsed by Bloxstrap Labs or Roblox Corporation.")
                .foregroundStyle(Brand.secondaryText)
                .fixedSize(horizontal: false, vertical: true)

            VStack(alignment: .leading, spacing: 8) {
                Link("GitHub repository", destination: Brand.githubURL)
                Link("Releases", destination: Brand.releasesURL)
                Link("Report an issue", destination: Brand.issuesURL)
            }

            Text("Account cookies stay in the Keychain on this Mac. Only add accounts you own.")
                .font(.callout)
                .foregroundStyle(Brand.secondaryText)

            Spacer()
        }
    }
}
