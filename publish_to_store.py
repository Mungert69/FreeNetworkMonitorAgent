from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload
import sys

SERVICE_ACCOUNT_FILE = '/home/mahadeva/code/securefiles/gaccount.json'
SCOPES = ['https://www.googleapis.com/auth/androidpublisher']
PACKAGE_NAME = 'click.freenetworkmonitor.networkmonitormaui'
AAB_PATH = '/home/mahadeva/code/FreeNetworkMonitorAgent/bin/Release/net9.0-android/click.freenetworkmonitor.networkmonitormaui-Signed.aab'
TRACK = 'production'  # Change to 'internal', 'beta', or 'alpha' if needed

def main():
    try:
        print("Authenticating with Google Play API...")
        credentials = service_account.Credentials.from_service_account_file(
            SERVICE_ACCOUNT_FILE, scopes=SCOPES)
        service = build('androidpublisher', 'v3', credentials=credentials)

        # 1. Create an edit
        print("Creating a new edit transaction...")
        edit_request = service.edits().insert(body={}, packageName=PACKAGE_NAME)
        result = edit_request.execute()
        edit_id = result['id']
        print(f"Created edit with ID: {edit_id}")

        # 2. Upload the AAB
        print(f"Uploading AAB: {AAB_PATH}")
        media = MediaFileUpload(
            AAB_PATH,
            mimetype='application/octet-stream',
            resumable=True
        )
        aab_response = service.edits().bundles().upload(
            editId=edit_id,
            packageName=PACKAGE_NAME,
            media_body=media
        ).execute()
        version_code = aab_response['versionCode']
        print(f"Uploaded AAB, versionCode: {version_code}")

        # 3. Assign to a release and track
        print(f"Assigning versionCode {version_code} to track '{TRACK}'...")
        track_response = service.edits().tracks().update(
            editId=edit_id,
            packageName=PACKAGE_NAME,
            track=TRACK,
            body={
                'releases': [{
                    'name': f'Release {version_code}',
                    'status': 'completed',
                    'versionCodes': [version_code],
                    'releaseNotes': [{
                        'language': 'en-GB',
                        'text': 'Automated upload .'
                    }]
                }]
            }
        ).execute()
        print(f"Assigned to track: {track_response['track']}")

        # 4. Commit the edit (publish)
        print("Committing/publishing ...")
        commit_request = service.edits().commit(editId=edit_id, packageName=PACKAGE_NAME).execute()
        print('✅ App uploaded and published ')

    except Exception as e:
        print(f"❌ Error: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()

