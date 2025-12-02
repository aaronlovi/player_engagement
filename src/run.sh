#!/usr/bin/env bash

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SOLUTION_PATH="${SCRIPT_DIR}/PlayerEngagement.sln"
HOST_URL="${HOST_URL:-http://localhost:5094}"
PING_PATH="/xp/policies/ping"

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
6) Run PlayerEngagement.Host
7) Verify policy ping endpoint

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
    start_time=$(date +%s.%2N)
    if dotnet format "${SOLUTION_PATH}" --verify-no-changes; then
        status="success"
    else
        status="failure"
    fi
    end_time=$(date +%s.%2N)
    duration=$(echo "${end_time} - ${start_time}" | bc)
    printf "Format verification %s in %.2f seconds\n" "${status}" "${duration}"
}

convert_line_endings() {
    (cd "${REPO_ROOT}" && find . -type f -name "*.cs" -exec unix2dos {} +)
}

run_host() {
    ASPNETCORE_URLS="${HOST_URL}" dotnet run --project "${REPO_ROOT}/src/PlayerEngagement.Host"
}

verify_policy_ping() {
    echo "Starting host at ${HOST_URL} for ping check..."
    ASPNETCORE_URLS="${HOST_URL}" dotnet run --project "${REPO_ROOT}/src/PlayerEngagement.Host" > /tmp/pe_host_ping.log 2>&1 &
    HOST_PID=$!
    echo "Waiting up to 20 seconds for host to warm up..."

    if command -v curl >/dev/null 2>&1; then
        success=0
        for attempt in $(seq 1 20); do
            echo "Attempt ${attempt}: hitting ${HOST_URL}${PING_PATH}..."
            if curl -sS "${HOST_URL}${PING_PATH}"; then
                echo
                echo "Ping succeeded at ${HOST_URL}${PING_PATH}"
                success=1
                break
            fi
            sleep 1
        done
        if [[ ${success} -eq 0 ]]; then
            echo "Ping failed after multiple attempts. Host output:"
            cat /tmp/pe_host_ping.log
        fi
    else
        echo "curl not found; please hit ${HOST_URL}${PING_PATH} manually."
    fi

    kill "${HOST_PID}" >/dev/null 2>&1 || true
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
        6) run_host ;;
        7) verify_policy_ping ;;
        q|Q) echo "Goodbye!"; exit 0 ;;
        *)
            echo
            echo "Invalid option. Try again."
            ;;
    esac
    echo
done
