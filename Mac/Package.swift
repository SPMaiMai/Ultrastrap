// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "Ultrastrap",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(name: "Ultrastrap", targets: ["Ultrastrap"])
    ],
    targets: [
        .executableTarget(
            name: "Ultrastrap",
            path: "Sources/Ultrastrap",
            resources: [
                .process("Resources")
            ]
        )
    ]
)
