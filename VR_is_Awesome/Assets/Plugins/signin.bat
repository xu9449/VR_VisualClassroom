echo off
title Sign In to AltspaceVR
curl -v -d "user[email]=wbolctest01@gmail.com&user[password]=wbolctest" https://account.altvr.com/users/sign_in.json -c cookie
