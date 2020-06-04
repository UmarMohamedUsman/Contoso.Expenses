# https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-kubernetes

#!/bin/bash

# Modify for your environment.
# ACR_NAME: The name of your Azure Container Registry
# SERVICE_PRINCIPAL_NAME: Must be unique within your AD tenant
$ACR_NAME="umaracr"
$SERVICE_PRINCIPAL_NAME="umaracr-service-principal"

# Obtain the full registry ID for subsequent command args
$ACR_REGISTRY_ID=$(az acr show --name $ACR_NAME --query id --output tsv)

# Create the service principal with rights scoped to the registry.
# Default permissions are for docker pull access. Modify the '--role'
# argument value as desired:
# acrpull:     pull only
# acrpush:     push and pull
# owner:       push, pull, and assign roles
$SP_PASSWD=$(az ad sp create-for-rbac --name http://$SERVICE_PRINCIPAL_NAME --scopes $ACR_REGISTRY_ID --role acrpull --query password --output tsv)
$SP_APP_ID=$(az ad sp show --id http://$SERVICE_PRINCIPAL_NAME --query appId --output tsv)

# Output the service principal's credentials; use these in your services and
# applications to authenticate to the container registry.
echo "Service principal ID: $SP_APP_ID"
echo "Service principal password: $SP_PASSWD"

# Create an image pull secret with the following kubectl command:

kubectl create secret docker-registry umaracr-secret `
    --namespace default `
    --docker-server=umaracr.azurecr.io `
    --docker-username=$SP_APP_ID `
    --docker-password=$SP_PASSWD


# Once you've created the image pull secret, you can use it to create Kubernetes pods and deployments. 
# Provide the name of the secret under imagePullSecrets in the deployment file. For example:
apiVersion: v1
kind: Pod
metadata:
  name: my-awesome-app-pod
  namespace: awesomeapps
spec:
  containers:
    - name: main-app-container
      image: myregistry.azurecr.io/my-awesome-app:v1
      imagePullPolicy: IfNotPresent
  imagePullSecrets:
    - name: acr-secret