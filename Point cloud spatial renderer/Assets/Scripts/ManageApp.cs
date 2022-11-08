using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;
using System.Linq;

public class ManageApp : MonoBehaviour
{

    //Definition of variables

    //States of FSM
    private enum stateVar
    {
        initial_screen,
        explanation_screen,
        training,
        end_training,
        begin_depth,
        rotating, 
        stopped, 
        rating,
        final_screen, 
        jumping
    }
    private stateVar state;

    //Rotation of point clouds
    public float rotation_seconds = 12;
    public float stopped_seconds = 1;
    public int FPS = 120;
    private float ang_inc;
    private float angular_position;
    private int number_rotations;
    private int stimuli_number = 0;
    private int max_rotations = 1;
    private float time_stopped = 0;

    //Text GameObjects
    private GameObject debugTextObject, messageRateObject, stimulusNumberObject, messageIdObject, inputIdObject, referenceTextObject, distortedTextObject, finalMessageObject, 
        endTrainingMessageObject, beginDepthMessageObject, questionDepthMessageObject;
    private GameObject[] rateTextObjects = new GameObject[5];
    private GameObject[] trainingMessageObjects = new GameObject[3];
    private GameObject[] instructionMessageObjects = new GameObject[5];
    private GameObject[] depthTextObjects = new GameObject[5];

    //Point cloud GameObjects
    private GameObject pcObject, pcObject_ref;

    //Names of stimuli
    private string[] ref_model_names = { "wooden_dragon_vox10_ref", "fruits_vox10_ref", "mitch_vox10_ref", "ipanemaCut_vox10_ref", "longdress_vox10_ref", "CITIUSP_vox10_ref" };
    private string[] stimuli = new string[64];
    private string stimuli_list;
    private string[] training_stimuli = new string[4];
    private string[] sorted_stimuli = new string[58];
    private int max_stimuli_lists = 100;
    private int list_pos = 1;

    //Point sizes
    private float[] point_sizes = new float[58];
    private float[] training_point_sizes = new float[4];

    //Positioning of point clouds
    private float pcs_distance = 0.06f;
    private float scene_factor = 1.3f;
    private float factor = 1.0f;

    //Registration process
    private string subject_id;
    private string subjects_list_path = "Assets/Logs/test_subjects.txt";

    //Instruction process
    private int instruction_counter = 0;

    //Training process
    private bool is_training;
    private int training_pos;

    //Main experiment
    private bool reference_right;
    private int rating_val = 0;

    //Naturalness rating
    private bool only_reference = false;
    
    //Jumping mechanism
    private string jump_number_string = "";

    //Reads the point size from the respective csv file
    void ReadPointSizesCsv(string pointSizesPath)
	{
        char[] separators = new char[] { '\n', '\r' };

        StreamReader reader = new StreamReader(pointSizesPath);
        string point_sizes_csv = reader.ReadToEnd();
        reader.Close();

        string[] point_sizes_lines = point_sizes_csv.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < point_sizes_lines.Length; i++)
        {
            string[] point_sizes_line = point_sizes_lines[i].Split(',');
            sorted_stimuli[i] = point_sizes_line[0];
            point_sizes[i] = float.Parse(point_sizes_line[1], CultureInfo.InvariantCulture.NumberFormat);
        }
    }

    //Reads the list of training stimuli
    void ReadTrainingStimuliCsv(string trainingStimuliPath)
    {
        char[] separators = new char[] { '\n', '\r' };

        StreamReader reader = new StreamReader(trainingStimuliPath);
        string training_stimuli_csv = reader.ReadToEnd();
        reader.Close();

        string[] point_sizes_lines = training_stimuli_csv.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < point_sizes_lines.Length; i++)
        {
            string[] point_sizes_line = point_sizes_lines[i].Split(',');
            training_stimuli[i] = point_sizes_line[0];
            training_point_sizes[i] = float.Parse(point_sizes_line[1], CultureInfo.InvariantCulture.NumberFormat);
        }

    }

    //Reads the list of stimuli of the main experiment
    void ReadStimuliListTxt(string stimuliListPath)
	{
        char[] separators = new char[] { '\n', '\r' };

        StreamReader reader = new StreamReader(stimuliListPath);
        stimuli_list = reader.ReadToEnd();

        stimuli = stimuli_list.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
        reader.Close();
    }

    //Positions a point cloud in the scene
    void ScaleAndPositionPc(string model_name)
	{
        pcObject = GameObject.Find(model_name);
        pcObject.GetComponent<MeshRenderer>().enabled = false;
        
        pcObject.transform.position = new Vector3(0.0f, 0.0f, 0.1f);

        if (model_name.Contains("CITIUSP") || model_name.Contains("ipanemaCut"))
		{
            pcObject.transform.position = new Vector3(0.0f, 0.05f, 0.1f);
            pcObject.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f) * scene_factor;
        }
		else
		{
            pcObject.transform.position = new Vector3(0.0f, 0.0f, 0.1f);
            pcObject.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
        }
            
        pcObject.transform.eulerAngles = new Vector3(0.0f, 180.0f, 0.1f);
    }

    

    //Identifies the number of the stimuli_list file to be used
    int findFirstListPos()
	{
        StreamReader reader = new StreamReader(subjects_list_path);
        string subjects_list = reader.ReadToEnd();
        reader.Close();

        int next_stimuli_list = 1;
        while (subjects_list.Contains("stimuli_list_" + next_stimuli_list.ToString() + ".txt"))
            next_stimuli_list++;

        if (next_stimuli_list > max_stimuli_lists)
            next_stimuli_list = 1;

        return next_stimuli_list;
    }

    //Function executed when the program is started
    void Start()
    {   
        ang_inc = 360f / (FPS * rotation_seconds);
        Application.targetFrameRate = FPS;

        //Reads information from files on disk
        string pointSizesPath = "Assets/Dataset/point_sizes/point_sizes.csv";
        ReadPointSizesCsv(pointSizesPath);

        string trainingStimuliPath = "Assets/Dataset/point_sizes/training_stimuli.csv";
        ReadTrainingStimuliCsv(trainingStimuliPath);

        list_pos = findFirstListPos();
        string stimuliListPath = "Assets/Dataset/stimuli_list/stimuli_list_" + list_pos.ToString() + ".txt";
        ReadStimuliListTxt(stimuliListPath);

        //Scale and position all point clouds
        for (int i = 0; i < sorted_stimuli.Length; i++)
            ScaleAndPositionPc(sorted_stimuli[i]);

        for (int i = 0; i < ref_model_names.Length; i++)
            ScaleAndPositionPc(ref_model_names[i]);

        for (int i = 0; i < training_stimuli.Length - 1; i++)
            ScaleAndPositionPc(training_stimuli[i]);

        ScaleAndPositionPc(training_stimuli[training_stimuli.Length - 1] + "_ref");

        //Assigns the text to all text GameObjects
        stimulusNumberObject = GameObject.Find("Stimulus number");
        stimulusNumberObject.GetComponent<TextMesh>().text = (stimuli_number + 1).ToString() + "/" + stimuli.Length.ToString();
        stimulusNumberObject.GetComponent<MeshRenderer>().enabled = false;

        messageRateObject = GameObject.Find("Message rate");
        messageRateObject.GetComponent<MeshRenderer>().enabled = false;

        messageIdObject = GameObject.Find("Message ID");
        messageIdObject.GetComponent<MeshRenderer>().enabled = true;

        inputIdObject = GameObject.Find("Input ID");
        inputIdObject.GetComponent<MeshRenderer>().enabled = true;

        debugTextObject = GameObject.Find("Debug Text");
        debugTextObject.GetComponent<TextMesh>().text = "debug text" ;
        debugTextObject.GetComponent<MeshRenderer>().enabled = false;

        referenceTextObject = GameObject.Find("Reference text");
        referenceTextObject.GetComponent<MeshRenderer>().enabled = false;

        distortedTextObject = GameObject.Find("Distorted text");
        distortedTextObject.GetComponent<MeshRenderer>().enabled = false;

        finalMessageObject = GameObject.Find("Final message");
        finalMessageObject.GetComponent<MeshRenderer>().enabled = false;

        endTrainingMessageObject = GameObject.Find("End training message");
        endTrainingMessageObject.GetComponent<MeshRenderer>().enabled = false;

        beginDepthMessageObject = GameObject.Find("Begin depth message");
        beginDepthMessageObject.GetComponent<MeshRenderer>().enabled = false;

        questionDepthMessageObject = GameObject.Find("Question depth message");
        beginDepthMessageObject.GetComponent<MeshRenderer>().enabled = false;

        for (int i = 0; i < rateTextObjects.Length; i++)
		{
            rateTextObjects[i] = GameObject.Find("Rate " + (i+1).ToString() + " text");
            rateTextObjects[i].GetComponent<TextMesh>().color = Color.grey;
            rateTextObjects[i].GetComponent<MeshRenderer>().enabled = false;
        }

        for (int i = 0; i < trainingMessageObjects.Length; i++)
        {
            trainingMessageObjects[i] = GameObject.Find("Training message " + (i + 1).ToString());
            trainingMessageObjects[i].GetComponent<MeshRenderer>().enabled = false;
        }

        for (int i = 0; i < instructionMessageObjects.Length; i++)
        {
            instructionMessageObjects[i] = GameObject.Find("Instruction message " + (i + 1).ToString());
            instructionMessageObjects[i].GetComponent<MeshRenderer>().enabled = false;
        }

        for (int i = 0; i < depthTextObjects.Length; i++)
        {
            depthTextObjects[i] = GameObject.Find("Depth " + (i + 1).ToString() + " text");
            depthTextObjects[i].GetComponent<TextMesh>().color = Color.grey;
            depthTextObjects[i].GetComponent<MeshRenderer>().enabled = false;
        }

        //Assigns the initial state of the FSM
        state = stateVar.initial_screen;
    }

    //Manages the input from the user when writing the subject ID
    void manageInputIdText()
    {
        if (Input.GetKeyDown("1") || Input.GetKeyDown(KeyCode.Keypad1))
        {
            inputIdObject.GetComponent<TextMesh>().text += "1";
        }
        if (Input.GetKeyDown("2") || Input.GetKeyDown(KeyCode.Keypad2))
        {
            inputIdObject.GetComponent<TextMesh>().text += "2";
        }
        if (Input.GetKeyDown("3") || Input.GetKeyDown(KeyCode.Keypad3))
        {
            inputIdObject.GetComponent<TextMesh>().text += "3";
        }
        if (Input.GetKeyDown("4") || Input.GetKeyDown(KeyCode.Keypad4))
        {
            inputIdObject.GetComponent<TextMesh>().text += "4";
        }
        if (Input.GetKeyDown("5") || Input.GetKeyDown(KeyCode.Keypad5))
        {
            inputIdObject.GetComponent<TextMesh>().text += "5";
        }
        if (Input.GetKeyDown("6") || Input.GetKeyDown(KeyCode.Keypad6))
        {
            inputIdObject.GetComponent<TextMesh>().text += "6";
        }
        if (Input.GetKeyDown("7") || Input.GetKeyDown(KeyCode.Keypad7))
        {
            inputIdObject.GetComponent<TextMesh>().text += "7";
        }
        if (Input.GetKeyDown("8") || Input.GetKeyDown(KeyCode.Keypad8))
        {
            inputIdObject.GetComponent<TextMesh>().text += "8";
        }
        if (Input.GetKeyDown("9") || Input.GetKeyDown(KeyCode.Keypad9))
        {
            inputIdObject.GetComponent<TextMesh>().text += "9";
        }
        if (Input.GetKeyDown("0") || Input.GetKeyDown(KeyCode.Keypad0))
        {
            inputIdObject.GetComponent<TextMesh>().text += "0";
        }
        if (Input.GetKeyDown("backspace"))
        {
            inputIdObject.GetComponent<TextMesh>().text = inputIdObject.GetComponent<TextMesh>().text.Remove(inputIdObject.GetComponent<TextMesh>().text.Length - 1, 1);
        }
    }

    //Manages the input from the user when rating a point cloud
    void manageInputRateText()
    {
        bool rateChanged = false;

        if (Input.GetKeyDown("1") || Input.GetKeyDown(KeyCode.Keypad1))
        {
            rateChanged = true;
            rating_val = 1;
        }
        if (Input.GetKeyDown("2") || Input.GetKeyDown(KeyCode.Keypad2))
        {
            rateChanged = true;
            rating_val = 2;
        }
        if (Input.GetKeyDown("3") || Input.GetKeyDown(KeyCode.Keypad3))
        {
            rateChanged = true;
            rating_val = 3;
        }
        if (Input.GetKeyDown("4") || Input.GetKeyDown(KeyCode.Keypad4))
        {
            rateChanged = true;
            rating_val = 4;
        }
        if (Input.GetKeyDown("5") || Input.GetKeyDown(KeyCode.Keypad5))
        {
            rateChanged = true;
            rating_val = 5;
        }

        if (rateChanged)
        {
            if (!only_reference)
            {
                for (int i = 0; i < rateTextObjects.Length; i++)
                {
                    rateTextObjects[i].GetComponent<TextMesh>().color = Color.grey;
                }
                rateTextObjects[rating_val - 1].GetComponent<TextMesh>().color = Color.white;
            }
            else
            {
                for (int i = 0; i < depthTextObjects.Length; i++)
                {
                    depthTextObjects[i].GetComponent<TextMesh>().color = Color.grey;
                }
                depthTextObjects[rating_val - 1].GetComponent<TextMesh>().color = Color.white;
            }

            rateChanged = false;
        }        
    }

    //Manages the input from the user during the jumping state
    void manageInputJump()
    {
        if (Input.GetKeyDown("1") || Input.GetKeyDown(KeyCode.Keypad1))
        {
            jump_number_string += "1";
        }
        if (Input.GetKeyDown("2") || Input.GetKeyDown(KeyCode.Keypad2))
        {
            jump_number_string += "2";
        }
        if (Input.GetKeyDown("3") || Input.GetKeyDown(KeyCode.Keypad3))
        {
            jump_number_string += "3";
        }
        if (Input.GetKeyDown("4") || Input.GetKeyDown(KeyCode.Keypad4))
        {
            jump_number_string += "4";
        }
        if (Input.GetKeyDown("5") || Input.GetKeyDown(KeyCode.Keypad5))
        {
            jump_number_string += "5";
        }
        if (Input.GetKeyDown("6") || Input.GetKeyDown(KeyCode.Keypad6))
        {
            jump_number_string += "6";
        }
        if (Input.GetKeyDown("7") || Input.GetKeyDown(KeyCode.Keypad7))
        {
            jump_number_string += "7";
        }
        if (Input.GetKeyDown("8") || Input.GetKeyDown(KeyCode.Keypad8))
        {
            jump_number_string += "8";
        }
        if (Input.GetKeyDown("9") || Input.GetKeyDown(KeyCode.Keypad9))
        {
            jump_number_string += "9";
        }
        if (Input.GetKeyDown("0") || Input.GetKeyDown(KeyCode.Keypad0))
        {
            jump_number_string += "0";
        }
        if (Input.GetKeyDown("backspace"))
        {
            jump_number_string = jump_number_string.Remove(jump_number_string.Length - 1, 1);
        }
    }


    //Adds subject ID and related stimuli_list file to the log after registration 
    void AddSubjectLog()
	{
        StreamWriter writer = new StreamWriter(subjects_list_path, true);
        writer.WriteLine(inputIdObject.GetComponent<TextMesh>().text + ", stimuli_list_" + list_pos.ToString() + ".txt");
        writer.Close();
    }

    //Removes last subject ID from the log
    void removeLastSubjectLog()
    {
        StreamReader reader = new StreamReader(subjects_list_path);
        string subjects_list = reader.ReadToEnd();
        reader.Close();

        char[] separators = new char[] { '\n', '\r' };
        string[] subjects_list_lines = subjects_list.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);

        StreamWriter writer = new StreamWriter(subjects_list_path, false);
        for (int i = 0; i < subjects_list_lines.Length - 1; i++)
            writer.WriteLine(subjects_list_lines[i]);
        writer.Close();
    }

    //Enables the point cloud specified by model_name in the screen
    void enablePointCloud(string model_name, bool training)
    {
        int stimuli_csv_position;
		string[] stimuli_list;
		float[] point_size_list;
		float disk_size;
		
		if (!training) 
		{
			stimuli_list = sorted_stimuli;
			point_size_list = point_sizes;
		}
		else
		{
			stimuli_list = training_stimuli;
			point_size_list = training_point_sizes;
		}

        if (!only_reference)
        {
            if (model_name.Contains("CITIUSP") || model_name.Contains("ipanemaCut"))
                factor = scene_factor;
            else
                factor = 1.0f;

            pcObject = GameObject.Find(model_name);
            pcObject.GetComponent<MeshRenderer>().enabled = true;

            stimuli_csv_position = 0;
            while (model_name != stimuli_list[stimuli_csv_position])
            {
                stimuli_csv_position++;
                if (stimuli_csv_position == stimuli_list.Length)
                    break;
            }
            if (stimuli_csv_position < stimuli_list.Length)
            {
                disk_size = point_size_list[stimuli_csv_position] * factor;
            }
            else
            {
                disk_size = 0.0001f;
            }
            pcObject.GetComponent<MeshRenderer>().material.SetFloat("_PointSize", disk_size);
        }

        string[] model_name_split = model_name.Split('-');
        string model_name_orig = model_name_split[0];
        string model_name_ref = model_name_orig + "_ref";

        pcObject_ref = GameObject.Find(model_name_ref);
        pcObject_ref.GetComponent<MeshRenderer>().enabled = true;
        stimuli_csv_position = 0;
        while (model_name_orig != stimuli_list[stimuli_csv_position])
        {
            stimuli_csv_position++;
            if (stimuli_csv_position == stimuli_list.Length)
                break;
        }
        if (stimuli_csv_position < stimuli_list.Length)
        {
            disk_size = point_size_list[stimuli_csv_position] * factor;
        }
        else
        {
            disk_size = 0.0001f;
        }
        pcObject_ref.GetComponent<MeshRenderer>().material.SetFloat("_PointSize", disk_size);

        if (!only_reference)
		{
            float sign_reference = 1.0f, sign_distorted = 1.0f;

            if (reference_right)
                sign_distorted = -1.0f;
            else
                sign_reference = -1.0f;

            pcObject.transform.Translate(new Vector3(sign_distorted * pcs_distance, 0, 0) * factor, Space.World);
            pcObject_ref.transform.Translate(new Vector3(sign_reference * pcs_distance, 0, 0) * factor, Space.World);

            distortedTextObject.GetComponent<MeshRenderer>().enabled = true;
            distortedTextObject.transform.Translate(new Vector3(sign_distorted * pcs_distance, 0, 0) * factor, Space.World);

            referenceTextObject.GetComponent<MeshRenderer>().enabled = true;
            referenceTextObject.transform.Translate(new Vector3(sign_reference * pcs_distance, 0, 0) * factor, Space.World);
        }
    }

    //Disable the point cloud specified by model_name from the screen
    void disablePointCloud()
	{
        pcObject_ref.GetComponent<MeshRenderer>().enabled = false;

        if (!only_reference)
		{
            float sign_reference = 1.0f, sign_distorted = 1.0f;

            if (reference_right)
                sign_distorted = -1.0f;
            else
                sign_reference = -1.0f;

            pcObject.transform.Translate(new Vector3(-sign_distorted * pcs_distance, 0, 0) * factor, Space.World);
            pcObject.GetComponent<MeshRenderer>().enabled = false;

            pcObject_ref.transform.Translate(new Vector3(-sign_reference * pcs_distance, 0, 0) * factor, Space.World);

            distortedTextObject.GetComponent<MeshRenderer>().enabled = false;
            distortedTextObject.transform.Translate(new Vector3(-sign_distorted * pcs_distance, 0, 0) * factor, Space.World);

            referenceTextObject.GetComponent<MeshRenderer>().enabled = false;
            referenceTextObject.transform.Translate(new Vector3(-sign_reference * pcs_distance, 0, 0) * factor, Space.World);
        }

    }

    //Adds the rating given by the subject to the log
    void addScoreLine(string score_log_path, string pc_name, int rating)
	{
        StreamWriter writer = new StreamWriter(score_log_path, true);
        writer.WriteLine(pc_name + "," + rating.ToString());
        writer.Close();
    }

    // Update is called once per frame
    void Update()
    {

		switch (state)
		{
            //During the initial_screen state, the application waits for the subject to fill their ID
            case stateVar.initial_screen:

                manageInputIdText();

                if (Input.GetKeyDown(KeyCode.Return) && inputIdObject.GetComponent<TextMesh>().text.Length > 0)
				{

                    AddSubjectLog();

                    subject_id = inputIdObject.GetComponent<TextMesh>().text;

                    inputIdObject.GetComponent<TextMesh>().text = "";
                    inputIdObject.GetComponent<MeshRenderer>().enabled = false;

                    messageIdObject.GetComponent<MeshRenderer>().enabled = false;

                    instruction_counter = 0;
                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = true;

                    state = stateVar.explanation_screen;

                    var rand = new System.Random();
                    reference_right = (rand.NextDouble() >= 0.5);
                }

                break;
            //During the explanation_screen state, the application shows the instructions and goes from one screen to another as the 
            //subjects presses left and right
            case stateVar.explanation_screen:

                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Backspace))
				{
                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = false;
                    removeLastSubjectLog();

                    state = stateVar.jumping;
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
				{
                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = false;

                    instruction_counter++;
                    if (instruction_counter == instructionMessageObjects.Length)
                        instruction_counter--;

                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = true;
                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = false;

                    instruction_counter--;
                    if (instruction_counter < 0)
                        instruction_counter++;

                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = true;
                }

                if (Input.GetKeyDown(KeyCode.Return) && (instruction_counter == instructionMessageObjects.Length - 1))
                {
                    instructionMessageObjects[instruction_counter].GetComponent<MeshRenderer>().enabled = false;

                    is_training = true;
                    training_pos = 0;

                    trainingMessageObjects[training_pos].GetComponent<MeshRenderer>().enabled = true;

                    state = stateVar.training;
                }

                break;
            //During the training state, a message regarding the current training step is shown in the screen 
                //until the user presses Enter
            case stateVar.training:

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    enablePointCloud(training_stimuli[training_pos], true);
                    trainingMessageObjects[training_pos].GetComponent<MeshRenderer>().enabled = false;
                    state = stateVar.rotating;
                }

                break;
            //During the end_training state, a message regarding the end of the training phase is shown in the screen 
                //until the user presses Enter
            case stateVar.end_training:

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    enablePointCloud(stimuli[stimuli_number], false);

                    endTrainingMessageObject.GetComponent<MeshRenderer>().enabled = false;
                    stimulusNumberObject.GetComponent<MeshRenderer>().enabled = true;

                    state = stateVar.rotating;
                }

                break;
            //During the rotating state, the point cloud shown in the screen is rotated at a fixed angular speed
            case stateVar.rotating:
                pcObject.transform.Rotate(0.0f, ang_inc, 0.0f, Space.World);
                pcObject_ref.transform.Rotate(0.0f, ang_inc, 0.0f, Space.World);

                angular_position += ang_inc;

                if (angular_position >= 360.0f)
                {
                    angular_position -= 360.0f;
                    number_rotations += 1;

                    if (number_rotations >= max_rotations)
                    {
                        state = stateVar.stopped;
                    }
                }
                break;
            //During the stopped state, the point cloud shown in the screen is stopped for a fixed number of seconds
            case stateVar.stopped:
                time_stopped += 1.0f / FPS;

                if (time_stopped >= stopped_seconds)
                {
                    time_stopped = 0;
                    state = stateVar.rating;
                    disablePointCloud();

                    if (!only_reference)
					{
                        messageRateObject.GetComponent<MeshRenderer>().enabled = true;

                        for (int i = 0; i < rateTextObjects.Length; i++)
                        {
                            rateTextObjects[i].GetComponent<MeshRenderer>().enabled = true;
                        }
                    }
					else
					{
                        questionDepthMessageObject.GetComponent<MeshRenderer>().enabled = true;

                        for (int i = 0; i < rateTextObjects.Length; i++)
                        {
                            depthTextObjects[i].GetComponent<MeshRenderer>().enabled = true;
                        }
                    }
                
                    stimulusNumberObject.GetComponent<MeshRenderer>().enabled = false;
                }
                break;
            //During the rating state, the subject is able to rate the point cloud that was just displayed in the screen
            case stateVar.rating:

                manageInputRateText();

                if (Input.GetKeyDown(KeyCode.Return) && (rating_val > 0))
                {
                    string score_log_path = "Assets/Logs/scores/" + subject_id + ".txt";

                    debugTextObject.GetComponent<TextMesh>().text = "Score given";
                    if (is_training)
                        addScoreLine(score_log_path, training_stimuli[training_pos], rating_val);
                    else
                        addScoreLine(score_log_path, stimuli[stimuli_number], rating_val);

                    rating_val = 0;

                    if (!only_reference)
					{
                        for (int i = 0; i < rateTextObjects.Length; i++)
                        {
                            rateTextObjects[i].GetComponent<TextMesh>().color = Color.grey;
                            rateTextObjects[i].GetComponent<MeshRenderer>().enabled = false;
                        }

                        messageRateObject.GetComponent<MeshRenderer>().enabled = false;
                    }
					else
					{
                        for (int i = 0; i < depthTextObjects.Length; i++)
                        {
                            depthTextObjects[i].GetComponent<TextMesh>().color = Color.grey;
                            depthTextObjects[i].GetComponent<MeshRenderer>().enabled = false;
                        }

                        questionDepthMessageObject.GetComponent<MeshRenderer>().enabled = false;
                    }

                    if (is_training)
                    {
                        debugTextObject.GetComponent<TextMesh>().text = "Score given training";
                        training_pos++;

                        if (training_pos == training_stimuli.Length - 1)
                        {
                            training_pos = 0;
                            is_training = false;
                            endTrainingMessageObject.GetComponent<MeshRenderer>().enabled = true;
                            state = stateVar.end_training;
                        }
						else
						{
                            trainingMessageObjects[training_pos].GetComponent<MeshRenderer>().enabled = true;
                            state = stateVar.training;
                        }

                    }
					else
					{
                        stimuli_number++;

                        if (stimuli_number == stimuli.Length)
                        {
                            stimuli_number = 0;
                            list_pos++;
                            string stimuliListPath = "Assets/Dataset/stimuli_list/stimuli_list_" + list_pos.ToString() + ".txt";
                            ReadStimuliListTxt(stimuliListPath);

                            stimulusNumberObject.GetComponent<TextMesh>().text = (stimuli_number + 1).ToString() + "/" + stimuli.Length.ToString();
                            only_reference = false;

                            referenceTextObject.GetComponent<MeshRenderer>().enabled = false;
                            distortedTextObject.GetComponent<MeshRenderer>().enabled = false;
                            finalMessageObject.GetComponent<MeshRenderer>().enabled = true;

                            state = stateVar.final_screen;
                        }
						else
						{
                            if (stimuli_number == stimuli.Length - 6)
							{
                                only_reference = true;
                                beginDepthMessageObject.GetComponent<MeshRenderer>().enabled = true;

                                state = stateVar.begin_depth;
                            }
							else
							{
                                enablePointCloud(stimuli[stimuli_number], false);

                                stimulusNumberObject.GetComponent<TextMesh>().text = (stimuli_number + 1).ToString() + "/" + stimuli.Length.ToString();
                                stimulusNumberObject.GetComponent<MeshRenderer>().enabled = true;

                                state = stateVar.rotating;
                            }
                        }
                    }                    
                }
                break;
            //During the begin_depth state, a message regarding the beginning of the naturalness evaluation is displayed in the screen
                //until the subject presses Enter
            case stateVar.begin_depth:
                if (Input.GetKeyDown(KeyCode.Return))
				{
                    enablePointCloud(stimuli[stimuli_number], false);

                    stimulusNumberObject.GetComponent<TextMesh>().text = (stimuli_number + 1).ToString() + "/" + stimuli.Length.ToString();
                    stimulusNumberObject.GetComponent<MeshRenderer>().enabled = true;
                    beginDepthMessageObject.GetComponent<MeshRenderer>().enabled = false;

                    state = stateVar.rotating;
                }
                break;

            //During the final_screen state, a message regarding the end of the evaluation is displayed in the screen
            case stateVar.final_screen:

                if (Input.GetKeyDown(KeyCode.Return))
				{
                    finalMessageObject.GetComponent<MeshRenderer>().enabled = false;
                    messageIdObject.GetComponent<MeshRenderer>().enabled = true;
                    inputIdObject.GetComponent<MeshRenderer>().enabled = true;

                    state = stateVar.initial_screen;
                }

                break;

            //During the jumping state, the program allows for the evaluation to jump to a specific step of the evaluation that is
                //determined by an numeric input by the user. This functionality is "hidden" and allows for carry on an evaluation 
                //that was started previously but for some reason was stopped in the middle
            case stateVar.jumping:
                manageInputJump();

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    list_pos--;
                    string newstimuliListPath = "Assets/Dataset/stimuli_list/stimuli_list_" + list_pos.ToString() + ".txt";
                    ReadStimuliListTxt(newstimuliListPath);
                    stimuli_number = System.Int32.Parse(jump_number_string) - 1;

                    enablePointCloud(stimuli[stimuli_number], false);

                    stimulusNumberObject.GetComponent<TextMesh>().text = (stimuli_number + 1).ToString() + "/" + stimuli.Length.ToString();
                    stimulusNumberObject.GetComponent<MeshRenderer>().enabled = true;

                    state = stateVar.rotating;
                }
                break;
        }
    }
}
