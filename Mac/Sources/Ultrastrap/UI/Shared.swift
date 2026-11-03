import SwiftUI

struct CardButton: View {
    let title: String
    var subtitle: String? = nil
    var systemImage: String
    var enabled: Bool = true
    var action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 14) {
                Image(systemName: systemImage)
                    .font(.title2)
                    .foregroundStyle(Brand.accent)
                    .frame(width: 28)
                VStack(alignment: .leading, spacing: 2) {
                    Text(title)
                        .font(.headline)
                        .foregroundStyle(.white)
                    if let subtitle {
                        Text(subtitle)
                            .font(.caption)
                            .foregroundStyle(Brand.secondaryText)
                    }
                }
                Spacer()
                Image(systemName: "chevron.right")
                    .foregroundStyle(Brand.secondaryText)
            }
            .padding(14)
            .background(Brand.card)
            .clipShape(RoundedRectangle(cornerRadius: 12, style: .continuous))
        }
        .buttonStyle(.plain)
        .disabled(!enabled)
        .opacity(enabled ? 1 : 0.45)
    }
}

struct SectionCard<Content: View>: View {
    let title: String
    var subtitle: String? = nil
    @ViewBuilder var content: Content

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            VStack(alignment: .leading, spacing: 2) {
                Text(title)
                    .font(.headline)
                if let subtitle {
                    Text(subtitle)
                        .font(.caption)
                        .foregroundStyle(Brand.secondaryText)
                }
            }
            content
        }
        .padding(16)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Brand.card)
        .clipShape(RoundedRectangle(cornerRadius: 14, style: .continuous))
    }
}

struct StatusBar: View {
    let text: String
    var busy: Bool = false

    var body: some View {
        HStack(spacing: 8) {
            if busy {
                ProgressView()
                    .controlSize(.small)
            }
            Text(text)
                .font(.callout)
                .foregroundStyle(Brand.secondaryText)
                .lineLimit(2)
            Spacer()
        }
        .padding(.horizontal, 4)
    }
}
