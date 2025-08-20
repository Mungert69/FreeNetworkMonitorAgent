#!/usr/bin/env python3
"""
Upload a signed Android App Bundle (.aab) to Google Play
and publish it to the specified track (alpha | beta | production | internal | rollout).

Example:
    python upload_play.py \
        --service-account-file /home/mahadeva/code/securefiles/gaccount.json \
        --package-name click.freenetworkmonitor.quantumsecure \
        --aab /home/mahadeva/code/NetworkMonitorQuantumSecure/bin/Release/net9.0-android/click.freenetworkmonitor.quantumsecure-Signed.aab \
        --track beta \
        --notes "Automated nightly build"
"""
import argparse
import sys
from pathlib import Path

from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

VALID_TRACKS = {"alpha", "beta", "production", "internal", "rollout"}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Upload & publish AAB to Google Play")
    parser.add_argument(
        "--service-account-file",
        required=True,
        type=Path,
        help="Path to Google service-account JSON key",
    )
    parser.add_argument(
        "--package-name",
        required=True,
        help="Application package name (e.g. com.example.app)",
    )
    parser.add_argument(
        "--aab",
        required=True,
        type=Path,
        help="Path to signed .aab file",
    )
    parser.add_argument(
        "--track",
        default="production",
        choices=sorted(VALID_TRACKS),
        help="Target track on Google Play (default: production)",
    )
    parser.add_argument(
        "--notes",
        default="Automated upload.",
        help="Release notes text (English, UK)",
    )
    return parser.parse_args()


def upload_aab(
    service_account_file: Path,
    package_name: str,
    aab_path: Path,
    track: str,
    release_notes: str,
) -> None:
    print("▶ Authenticating with Google Play…")
    credentials = service_account.Credentials.from_service_account_file(
        service_account_file, scopes=["https://www.googleapis.com/auth/androidpublisher"]
    )
    service = build("androidpublisher", "v3", credentials=credentials)

    print("▶ Creating a new edit transaction…")
    edit = service.edits().insert(body={}, packageName=package_name).execute()
    edit_id = edit["id"]
    print(f"   ✔ Edit ID: {edit_id}")

    print(f"▶ Uploading AAB: {aab_path}")
    media = MediaFileUpload(aab_path, mimetype="application/octet-stream", resumable=True)
    bundle = (
        service.edits()
        .bundles()
        .upload(editId=edit_id, packageName=package_name, media_body=media)
        .execute()
    )
    version_code = bundle["versionCode"]
    print(f"   ✔ Uploaded versionCode: {version_code}")

    print(f"▶ Assigning version {version_code} to track “{track}”…")
    service.edits().tracks().update(
        editId=edit_id,
        packageName=package_name,
        track=track,
        body={
            "releases": [
                {
                    "name": f"Release {version_code}",
                    "status": "completed",
                    "versionCodes": [version_code],
                    "releaseNotes": [
                        {
                            "language": "en-GB",
                            "text": release_notes,
                        }
                    ],
                }
            ]
        },
    ).execute()
    print("   ✔ Track updated")

    print("▶ Committing the edit (publishing)…")
    service.edits().commit(editId=edit_id, packageName=package_name).execute()
    print("🎉  App uploaded and published!")


def main() -> None:
    args = parse_args()

    # Early validation
    missing = [
        str(p) for p in (args.service_account_file, args.aab) if not p.is_file()
    ]
    if missing:
        print(f"❌ File(s) not found: {', '.join(missing)}", file=sys.stderr)
        sys.exit(2)

    try:
        upload_aab(
            service_account_file=args.service_account_file,
            package_name=args.package_name,
            aab_path=args.aab,
            track=args.track,
            release_notes=args.notes,
        )
    except Exception as exc:
        print(f"❌ Error: {exc}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

