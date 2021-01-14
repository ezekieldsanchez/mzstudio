using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MySql.Data.MySqlClient;

public class MySQL : MonoBehaviour
{
    public Test test;
    public Button additionSettingsButton;
    public GameObject verificitaionPanel;
    public Text verificationText;
    [Space(20)]
    public AudioClip confirmedBeep;
    public AudioClip errorBeep;

    [HideInInspector]
    public string connString = "";

    AudioSource myAudioSource;
    WaitForSeconds waitTime;
    bool isQRVerified;

    MySqlConnection conn;
    MySqlCommand command;
    MySqlDataReader reader;

    private void Awake()
    {
        waitTime = new WaitForSeconds(1.25f);
        myAudioSource = GetComponent<AudioSource>();
        if (ES3.KeyExists("employeeID"))
        {
            EmployeeDetails.employeeID = ES3.Load<string>("employeeID");
            test.scannerIDInput.text = EmployeeDetails.employeeID;
            additionSettingsButton.interactable = true;
        }
        else
        {
            EmployeeDetails.employeeID = "118238";
        }

        connString = string.Format("server={0};port={1};userid={2};password={3};database={4};",
            EmployeeDetails.Server, EmployeeDetails.port, EmployeeDetails.Username, EmployeeDetails.Password, EmployeeDetails.DatabaseName);

    }

    public void CheckContactID(string scannerID, string contactID)
    {
        conn = new MySqlConnection(connString);
        command = new MySqlCommand("SELECT id FROM employee where id=" + "'" + contactID + "'", conn);
        
        try
        {
            conn.Open();
            reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    int t = reader.GetInt32(0);

                    if (t.ToString().Trim() == contactID.Trim())
                    {
                        isQRVerified = true;
                        StopCoroutine(ShowVerificationPanel("QR Code Valid."));
                        StartCoroutine(ShowVerificationPanel("QR Code Valid."));
                        reader.Close();
                        conn.Close();

                        conn.Open();

                        MySqlCommand insertCom = new MySqlCommand("INSERT INTO qrtracker (scanner,contact) VALUES (" + "'" + scannerID + "'" + "," + "'" + contactID + "'" + ")", conn);

                        insertCom.Prepare();
                        insertCom.ExecuteNonQuery();

                        insertCom.Dispose();
                        isQRVerified = true;
                        StopCoroutine(ShowVerificationPanel("QR Code Uploaded to server."));
                        StartCoroutine(ShowVerificationPanel("QR Code Uploaded to server."));

                        break;
                    }
                }
            }
            else
            {
                isQRVerified = false;
                StopCoroutine(ShowVerificationPanel("Invalid QR Code \n (Employee ID)"));

                StartCoroutine(ShowVerificationPanel("Invalid QR Code \n (Employee ID)"));
            }

            reader.Close();

        }
        catch (MySqlException ex)
        {
            isQRVerified = false;
            StopCoroutine(ShowVerificationPanel(ex.Message));

            StartCoroutine(ShowVerificationPanel(ex.Message));
        }
        finally
        {
            command.Dispose();
            conn.Close();
        }

    }

    public IEnumerator ConnectToServer(string scannerID)
    {
        conn = new MySqlConnection(connString);
        command = new MySqlCommand("SELECT id FROM employee WHERE id = " + "'" + scannerID.Trim() + "'" + ";", conn);

        try
        {
            conn.Open();
            reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    int t = reader.GetInt32(0);

                    if (t.ToString().Trim() == scannerID.Trim())
                    {
                        isQRVerified = true;
                        StopCoroutine(ShowVerificationPanel("Employee ID Verified \n and Saved!"));
                        StartCoroutine(ShowVerificationPanel("Employee ID Verified \n and Saved!"));
                        SaveData(scannerID);
                        break;
                    }
                }
            }
            else
            {
                isQRVerified = false;
                StopCoroutine(ShowVerificationPanel("Invalid \n Employee ID"));
                StartCoroutine(ShowVerificationPanel("Invalid \n Employee ID"));
            }

            reader.Close();
            
        }
        catch (MySqlException e)
        {
            StopCoroutine(ShowVerificationPanel(e.Message));
            StartCoroutine(ShowVerificationPanel(e.Message));
        }
        finally
        {
            command.Dispose();
            conn.Close();
        }

        yield return null;

    }

    public void SaveData(string scannerID)
    {
        ES3.Save("employeeID", scannerID);

    }

    IEnumerator ShowVerificationPanel(string message)
    {
        if (isQRVerified)
        {
            verificitaionPanel.SetActive(true);
            verificitaionPanel.GetComponent<Image>().color = Color.green;
            verificationText.text = message;
            myAudioSource.PlayOneShot(confirmedBeep);
            yield return waitTime;
            verificitaionPanel.SetActive(false);
            isQRVerified = false;
        }
        else
        {
            verificitaionPanel.SetActive(true);
            verificitaionPanel.GetComponent<Image>().color = Color.red;
            verificationText.text = message;
            myAudioSource.PlayOneShot(errorBeep);
            yield return waitTime;
            verificitaionPanel.SetActive(false);
        }
    }
}
