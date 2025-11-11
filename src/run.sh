#!/usr/bin/env bash

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SOLUTION_PATH="${SCRIPT_DIR}/PlayerEngagement.sln"

print_menu() {
    cat <<'MENU'
===========================
 Player Engagement Runner
===========================
1) Clean solution
2) Build solution
3) Run unit tests
4) Verify formatting
5) Convert .cs files to CRLF

Q) Quit

MENU
    printf "Select an option: "
}

run_clean() {
    dotnet clean "${SOLUTION_PATH}"
}

run_build() {
    dotnet build "${SOLUTION_PATH}"
}

run_tests() {
    dotnet test "${SOLUTION_PATH}"
}

run_format_check() {
    dotnet format "${SOLUTION_PATH}" --verify-no-changes
}

convert_line_endings() {
    (cd "${REPO_ROOT}" && find . -type f -name "*.cs" -exec unix2dos {} +)
}

while true; do
    echo
    print_menu
    read -r choice
    case "${choice}" in
        1) run_clean ;;
        2) run_build ;;
        3) run_tests ;;
        4) run_format_check ;;
        5) convert_line_endings ;;
        q|Q) echo "Goodbye!"; exit 0 ;;
        *)
            echo
            echo "Invalid option. Try again."
            ;;
    esac
    echo
done
