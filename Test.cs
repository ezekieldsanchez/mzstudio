using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject wwwErrorImage;
    [SerializeField] GameObject verificationImage;
    [SerializeField] GameObject additionalSetinngsImage;
    [SerializeField] GameObject passwordImage;
    [SerializeField] GameObject connectionSettingsVerificationPanel;
    [Space(20)]
    [SerializeField] Text wwwErrorText;
    public InputField scannerIDInput;
    [SerializeField] InputField passInput;
    [SerializeField] Text verificationText;
    [SerializeField] Text connsettingsVerificationText;
    [Space(20)]
    [SerializeField] MySQL mySQLConn;
    [Space(20)]
    [Header("Connection Settings Inputfield")]
    [SerializeField] InputField server;
    [SerializeField] InputField database;
    [SerializeField] InputField username;
    [SerializeField] InputField password;
    [SerializeField] InputField port;
    [SerializeField] InputField settingsPass;

    WaitForSeconds closeVerificationImageTime;

    bool isScannerIDValid;
    bool isContactIDValid;
    bool isQRSubmitted;

    const string validID = "1";
    string idVerified = "Employee ID verified!";
    string idNotFound = "ID not found!\n Contact support.";
    string QRSubmitted = "QR Code Scanned and has been submitted!";

    private void Awake()
    {
        closeVerificationImageTime = new WaitForSeconds(3);

        LoadConnSettings();
        SetInputfields();
    }
    private void Start()
    {
        scannerIDInput.text = EmployeeDetails.employeeID;
    }

    #region http links used
    IEnumerator GetUsers(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            }
            else
            {
                Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
            }
        }
    }

    IEnumerator Login(string scannerId)
    {
        WWWForm form = new WWWForm();
        form.AddField("scannerID", scannerId);

        using (UnityWebRequest www = UnityWebRequest.Post("HTTP://storage.googleapis.com/unitytutorial/Login.php", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                StartCoroutine(ShowWWWErrorImage(www.error));
            }
            else
            {
                isScannerIDValid = www.downloadHandler.text.Contains(validID);
                StopCoroutine(ShowVerificationPanel());
                EmployeeDetails.employeeID = scannerIDInput.text;
                StartCoroutine(ShowVerificationPanel());
                
            }
        }
    }

    public IEnumerator CheckContactID(string contactID)
    {
        WWWForm form = new WWWForm();
        form.AddField("contactID", contactID);

        using (UnityWebRequest www = UnityWebRequest.Post(
            "HTTP://localhost/UnityBackEndTutorial/CheckContactID.php", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                StartCoroutine(ShowWWWErrorImage(www.error));
            }
            else
            {
                isContactIDValid = www.downloadHandler.text.Contains(validID);

                if (isContactIDValid && isScannerIDValid)
                {
                    StartCoroutine(SubmitQR(EmployeeDetails.employeeID, contactID));
                }
            }
        }
    }

    public IEnumerator SubmitQR(string scannerID, string contactID)
    {
        WWWForm form = new WWWForm();
        form.AddField("scannerID", scannerID);
        form.AddField("contactID", contactID);

        using (UnityWebRequest www = UnityWebRequest.Post(
            "HTTP://localhost/UnityBackEndTutorial/SubmitQR.php", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                StartCoroutine(ShowWWWErrorImage(www.error));
            }
            else
            {
                isQRSubmitted = www.downloadHandler.text.Contains(validID);
                if (isQRSubmitted)
                {
                    //dataTxtOutput.text = QRSubmitted;
                }
            }
        }
    }
    #endregion

    public void SaveSettings()
    {
        //mySQLConn.CheckScannerID(scannerIDInput.text,ShowVerificationPanel(),ShowWWWErrorImage("Invalid ID!"));
        StartCoroutine(mySQLConn.ConnectToServer(scannerIDInput.text));
        //mySQLConn.ConnectToServer(scannerIDInput.text);
    }

    public void CheckPassword()
    {
        if (EmployeeDetails.connPass == passInput.text)
        {
            additionalSetinngsImage.SetActive(true);
            passInput.text = "";
            passwordImage.SetActive(false);

        }
        else
        {
            Debug.Log("Incorrect Password");
        }
        
    }

    public void SaveConnectionSettings()
    {
        EmployeeDetails.Server = server.text;
        EmployeeDetails.DatabaseName = database.text;
        EmployeeDetails.Username = username.text;
        EmployeeDetails.Password = password.text;
        EmployeeDetails.port = port.text;
        EmployeeDetails.connPass = settingsPass.text;

        SaveConnSettings();
        LoadConnSettings();

        StopCoroutine(ShowConnPanel());
        StartCoroutine(ShowConnPanel());
    }

    public void SaveConnSettings()
    {
        ES3.Save("server", EmployeeDetails.Server);
        ES3.Save("databasename", EmployeeDetails.DatabaseName);
        ES3.Save("username", EmployeeDetails.Username);
        ES3.Save("pasword", EmployeeDetails.Password);
        ES3.Save("port", EmployeeDetails.port);
        ES3.Save("connpass", EmployeeDetails.connPass);
    }

    public void LoadConnSettings()
    {
        if (ES3.KeyExists("server")) EmployeeDetails.Server = ES3.Load<string>("server");
        if (ES3.KeyExists("databasename")) EmployeeDetails.DatabaseName = ES3.Load<string>("databasename");
        if (ES3.KeyExists("username")) EmployeeDetails.Username = ES3.Load<string>("username");
        if (ES3.KeyExists("pasword")) EmployeeDetails.Password = ES3.Load<string>("pasword");
        if (ES3.KeyExists("port")) EmployeeDetails.port = ES3.Load<string>("port");
        if (ES3.KeyExists("connpass")) EmployeeDetails.connPass = ES3.Load<string>("connpass");

        if (mySQLConn.connString == "" || mySQLConn.connString == null)
            mySQLConn.connString = string.Format("server={0};port={1};userid={2};password={3};database={4};",
            EmployeeDetails.Server, EmployeeDetails.port, EmployeeDetails.Username, EmployeeDetails.Password, EmployeeDetails.DatabaseName);
    }
    public void SetInputfields()
    {
        server.text = EmployeeDetails.Server;
        database.text = EmployeeDetails.DatabaseName;
        username.text = EmployeeDetails.Username;
        password.text = EmployeeDetails.Password;
        port.text = EmployeeDetails.port;
        settingsPass.text = EmployeeDetails.connPass;
    }

    IEnumerator ShowVerificationPanel()
    {
        verificationImage.SetActive(true);

        verificationText.text = idVerified;

        yield return closeVerificationImageTime;

        verificationImage.SetActive(false);

    }

    IEnumerator ShowWWWErrorImage(string message)
    {
        wwwErrorImage.SetActive(true);
        wwwErrorText.text = message;

        yield return closeVerificationImageTime;
        wwwErrorImage.SetActive(false);
    }

    IEnumerator ShowConnPanel()
    {
        connectionSettingsVerificationPanel.SetActive(true);

        yield return closeVerificationImageTime;
        connectionSettingsVerificationPanel.SetActive(false);

    }

}//class
