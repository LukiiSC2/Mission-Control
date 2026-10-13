{
  description = "MissionControl Avalonia .NET 10 application";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };

        dotnetPkg = pkgs.dotnetCorePackages.sdk_10_0;

        runtimeDeps = with pkgs; [
          # X11 / Wayland / Rendering
          xorg.libX11
          xorg.libXcursor
          xorg.libXext
          xorg.libXi
          xorg.libXrandr
          xorg.libXrender
          xorg.libXtst
          xorg.libICE
          xorg.libSM
          libGL
          libxkbcommon
          wayland

          # Fonts & Rendering
          fontconfig
          freetype
          harfbuzz

          # Audio / Media / DBus
          dbus
        ];

        # Avalonia VS Code extension packaged from VS Code Marketplace
        vscodeAvaloniaExt = pkgs.vscode-utils.buildVscodeMarketplaceExtension {
          mktplcRef = {
            name = "vscode-avalonia";
            publisher = "AvaloniaTeam";
            version = "12.4.3";
            hash = "sha256-JjcqeIsni8Slj25D8mw4SduZrjHRvkGhoo/sycgvfEU=";
          };
        };

        # Custom VS Code wrapper with Avalonia extension pre-installed
        vscodeWithAvalonia = pkgs.vscode-with-extensions.override {
          vscodeExtensions = [
            vscodeAvaloniaExt
          ];
        };
      in
      {
        packages = {
          default = vscodeWithAvalonia;
          vscode = vscodeWithAvalonia;
          vscode-avalonia = vscodeAvaloniaExt;
        };

        devShells.default = pkgs.mkShell {
          packages = [
            dotnetPkg
          ];

          env = {
            DOTNET_ROOT = "${dotnetPkg}";
            LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath runtimeDeps;
          };

          shellHook = ''
            echo "MissionControl (.NET 10 + Avalonia) dev shell ready"
            echo "Dotnet SDK: $(dotnet --version)"

            # Ensure VS Code has the Avalonia extension installed
            if command -v code >/dev/null 2>&1; then
              if ! code --list-extensions 2>/dev/null | grep -qi "avaloniateam.vscode-avalonia"; then
                echo "Installing Avalonia extension into VS Code..."
                code --install-extension AvaloniaTeam.vscode-avalonia --force >/dev/null 2>&1 || true
              fi
            fi
          '';
        };
      });
}
