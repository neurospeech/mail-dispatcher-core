# mail-dispatcher-core
.Net Core Email Dispatch MicroService to manage email queue, send reports with domain keys

# Features
1. Automatic Domain Key creation and signing
2. Automatic DNS resolution to find MX records
3. Try and connect to best possible MX and cache the result for 5 hours
4. Try Sending emails 3 times if there was a network issue
5. If server did not accept the message, an exception will be saved in `JobResponse`
6. Delete all jobs after 7 days
7. Save email in blob till the time it is trying to send
8. This app uses Eternity Framework, so after crash and restart, it will resume the operations

## Setup

1. You will need azure storage account and you can setup your key in `ConnectionStrings.AzureBlobs` in appsettings.json.
2. Application Insights must be configured to view the logs.
3. Change `AdminPassword` in appsettings.json
4. Setup `Smtp.Domain` that will be used in `HELO/EHLO` command, please make sure you setup correct reverse DNS for the same.

## Login

1. When you run it, it will open an empty index.html page.
2. Open `/swagger` to open API endpoints.
3. Click on `/api/auth/login` and Try to enter username `admin` and password that is configured in appsettings.json
4. Once you login, you can create new account by clicking `/api/accounts/new` and enter following 
```json
   {
       "id": "unique-alpha-numeric-key",
       "selector": "", /// keep same as id
       "domainName": "", /// from domain name
       "bounceTriggers": "" // [optional] multiple http rest end points for bounce notification separated by new line
   }
```
5. Above operation will generate public key for domainkey and auth key for REST Operations
6. This will also generate required domainkey with selector, you will need to setup the domain key in your DNS.

## Send Emails

1. Use AuthKey and id from the account generation process.
2. Simple, `api/queue/simple` will accept json to send html/text email. You can post attachments, for this, you will need to use form content type.
3. Raw, you can use MimeMessage from MimeKit to compose your email in mime format and send it.
4. When you send, you will get a job id in `id` field, you can use it to query status of your request.
