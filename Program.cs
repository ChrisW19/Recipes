using System.Text.Json;


string directoryPath = @"C:\Users\chris\source\repos\TodoList\BakingRecipes\bin\Debug\net8.0\";
string fileName = "CookieRecipe";
string recipeFilePath;
bool txtExists, jsonExists;

FindFileExtExist(directoryPath, fileName, out recipeFilePath, out txtExists, out jsonExists);
var ingredients = GetInitialIngredients();
recipeFilePath = FileExistDisplayRecipes(ingredients, directoryPath, fileName, txtExists, jsonExists);
CreateNewRecipe(ingredients, recipeFilePath);
DisplayExistingRecipes(recipeFilePath, ingredients);
Console.ReadLine();

static List<Ingredient> GetInitialIngredients() => new()
{
    new Wheat(1, "Wheat Flour", 500, "Add to mixture as directed."),
    new Coconut(2, "Coconut Flour", 250, "Add to mixture as directed."),
    new Butter(3, "Butter", 100, "Melt before adding."),
    new Chocolate(4, "Chocolate", 150, "Melt in a water bath."),
    new Sugar(5, "Sugar", 200, "Add directly to mixture."),
    new Cardamon(6, "Cardamon", 1, "Use 1/2 teaspoon."),
    new Cinnamon(7, "Cinnamon", 1, "Use 1/2 teaspoon."),
    new CocoaPowder(8, "Cocoa Powder", 50, "Add to mixture as directed.")
};

static string RecipeFileExist(string directoryPath, string fileName)
{
    // Default to the initial path if no changes
    string fileSave;

    Console.WriteLine("Recipe file not found.");
    Console.WriteLine("What file type do you want: [T]txt or [J]json?");
    string? userSelection = Console.ReadLine().ToUpper();

    switch (userSelection)
    {
        case "T":
            fileSave = directoryPath + fileName + ".txt";
            break;
        case "J":
            fileSave = directoryPath + fileName + ".json";
            break;
        default:
            Console.WriteLine("Invalid selection, default to .txt file");
            fileSave = directoryPath + fileName + ".txt";
            break;
    }

    try
    {
        File.Create(fileSave).Dispose();
        Console.WriteLine($"File created");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating file: {ex.Message}");
    }


    return fileSave; // Return the updated file path
}

void DisplayExistingRecipes(string recipeFilePath, List<Ingredient> availableIngredients)
{
    try
    {
        var recipeStr = File.ReadAllText(recipeFilePath);

        if (string.IsNullOrEmpty(recipeStr))
        {
            Console.WriteLine("Recipe file is empty.  Skipping showing existing recipes.");
            return;
        }

        List<Recipe> recipes = JsonSerializer.Deserialize<List<Recipe>>(recipeStr);
        if (recipes == null || recipes.Count == 0)
        {
            Console.WriteLine("No recipeds found in the file.");
        }

        Console.WriteLine("Previously saved recipe ingredients:");
        foreach (var recipe in recipes)
        {
            Console.WriteLine($"\n********* Recipe ID: {recipe.RecipeID} *********");

            foreach (var ingredient in recipe.Ingredients)
            {
                Console.WriteLine($"Name: {ingredient.Name}, Amount: {ingredient.Amount}, Instruction: {ingredient.Instruction}");
            }
        }

    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error deserializing recipe file: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading recipe file: {ex.Message}");
    }
}

void CreateNewRecipe(List<Ingredient> availableIngredients, string recipeFilePath)
{
    Console.WriteLine("\nCreate a new cookie recipe! Available ingredients:");
    availableIngredients.ForEach(Console.WriteLine);

    List<Ingredient> selectedIngredients;
    while (true)
    {
        Console.WriteLine("\nSelect ingredients by ID (comma-separated, unique, between 1-8): ");
        var inputIngredient = Console.ReadLine();

        selectedIngredients = ParseSelectedIngredients(inputIngredient, availableIngredients);

        // Validate the selected ingredients
        if (selectedIngredients.Count == 0)
        {
            Console.WriteLine("No valid ingredients were selected. Please try again.");
        }
        else if (selectedIngredients.Count != selectedIngredients.Distinct().Count())
        {
            Console.WriteLine("Duplicate ingredient IDs detected. Please select unique IDs.");
        }
        else
        {
            Console.WriteLine("\nYou Selected:");
            selectedIngredients.ForEach(ingredient =>
            {
                Console.WriteLine(ingredient);
                ingredient.Prepare();
            });
            break;
        }
    }

    AddRecipe(selectedIngredients, recipeFilePath);
}

List<Ingredient> ParseSelectedIngredients(string input, List<Ingredient> availableIngredients)
{
    var selectedIds = input?.Split(',') ?? Array.Empty<string>();
    var selectedIngredients = new List<Ingredient>();
    var unrecognizedIds = new List<string>();

    foreach (var idStr in selectedIds)
    {
        if (int.TryParse(idStr.Trim(), out int id))
        {
            var ingredient = availableIngredients.FirstOrDefault(i => i.ID == id);
            if (ingredient != null)
            {
                selectedIngredients.Add(ingredient);
            }
            else
            {
                unrecognizedIds.Add(idStr.Trim());
            }
        }
    }

    if (unrecognizedIds.Count > 0)
    {
        Console.WriteLine("\nUnrecognized IDs: " + string.Join(", ", unrecognizedIds));
    }

    return selectedIngredients;
}

void AddRecipe(List<Ingredient> ingredients, string filePath)
{
    try
    {
        int newRecipeId = -1;
        var recipeStr = File.ReadAllText(filePath);
        List<Recipe> existingRecipes;

        if (string.IsNullOrWhiteSpace(recipeStr))
        {
            // Initialize an empty list if the file is empty
            existingRecipes = new List<Recipe>();
        }
        else
        {
            try
            {
                existingRecipes = JsonSerializer.Deserialize<List<Recipe>>(recipeStr);
            }
            catch (JsonException)
            {
                // If it fails, attempt to deserialize as a single recipe and add it to a new list
                var singleRecipe = JsonSerializer.Deserialize<Recipe>(recipeStr);
                existingRecipes = new List<Recipe> { singleRecipe };
            }
        }

        // Set new recipe ID based on the last recipe's ID in the list, if any
        if (existingRecipes.Count > 0)
        {
            int lastRecipeID = existingRecipes.Last().RecipeID;
            newRecipeId = lastRecipeID + 1;
        }
        else
        {
            newRecipeId = 1;
        }

        // Create the new recipe and add it to the existing recipes list
        var newRecipe = new Recipe(newRecipeId, ingredients);
        existingRecipes.Add(newRecipe);

        string updatedRecipeJson = JsonSerializer.Serialize(existingRecipes);
        File.WriteAllText(filePath, updatedRecipeJson);

        Console.WriteLine("\nRecipe saved to file.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving recipe: {ex.Message}");
    }
}

string FileExistDisplayRecipes(List<Ingredient> ingredients, string directoryPath, string fileName, bool txtExists, bool jsonExists)
{
    string recipeFilePath;
    if (!txtExists && !jsonExists)
    {
        recipeFilePath = RecipeFileExist(directoryPath, fileName);
    }
    else
    {
        recipeFilePath = txtExists ? Path.Combine(directoryPath, fileName + ".txt") : Path.Combine(directoryPath, fileName + ".json");
        DisplayExistingRecipes(recipeFilePath, ingredients);
    }

    return recipeFilePath;
}

static void FindFileExtExist(string directoryPath, string fileName, out string recipeFilePath, out bool txtExists, out bool jsonExists)
{
    recipeFilePath = directoryPath + fileName;
    txtExists = File.Exists(Path.Combine(directoryPath, fileName + ".txt"));
    jsonExists = File.Exists(Path.Combine(directoryPath, fileName + ".json"));
}

public class Recipe
{
    public int RecipeID { get; set; }
    public List<Ingredient> Ingredients { get; set; }

    public Recipe(int recipeID, List<Ingredient> ingredients)
    {
        RecipeID = recipeID;
        Ingredients = ingredients ?? new List<Ingredient>(); // Ensure it's initialized
    }

    public void ListIngredients()
    {
        Console.WriteLine($"Recipe ID: {RecipeID}");
        Console.WriteLine("Ingredients:");
        foreach (var ingredient in Ingredients)
        {
            Console.WriteLine(ingredient);
        }
    }
}

public class Ingredient
{
    public Ingredient(int id, string name, int amount, string instruction)
    {
        ID = id;
        Name = name;
        Amount = amount;
        Instruction = instruction;
    }

    public int ID { get; }
    public int Amount { get; }
    public string Instruction { get; }

    public virtual string Name { get; }

    public virtual void Prepare() => Console.WriteLine(Instruction);

    public override string ToString() =>
        $"ID: {ID}, Name: {Name}, Amount: {Amount} grams, Instruction: {Instruction}";
}

public abstract class Flour : Ingredient
{
    public Flour(int id, string name, int weightInGrams, string instruction) : base(id, name, weightInGrams, instruction) { }

    public abstract string FlourType { get; }

    public override void Prepare() => Console.WriteLine($"{FlourType} - Sieve and add to other ingredients as directed.");
}

public class Wheat : Flour
{
    public Wheat(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override string FlourType => "Wheat";
    public override void Prepare() => Console.WriteLine($"{Name} - Sieve and mix with wet ingredients.");
}

public class Coconut : Flour
{
    public Coconut(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override string FlourType => "Coconut";
    public override void Prepare() => Console.WriteLine($"{Name} - Sieve, then fold gently into mixture.");
}

public class Butter : Ingredient
{
    public Butter(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override void Prepare() => Console.WriteLine("Melt on low heat. Add to other ingredients.");
}

public class Chocolate : Ingredient
{
    public Chocolate(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override void Prepare() => Console.WriteLine("Melt in a water bath. Add to other ingredients.");
}

public class Sugar : Ingredient
{
    public Sugar(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override void Prepare() => Console.WriteLine("Add to other ingredients.");
}

public class Cardamon : Ingredient
{
    public Cardamon(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override void Prepare() => Console.WriteLine("Take half a teaspoon. Add to other ingredients.");
}

public class Cinnamon : Ingredient
{
    public Cinnamon(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override void Prepare() => Console.WriteLine("Take half a teaspoon. Add to other ingredients.");
}

public class CocoaPowder : Ingredient
{
    public CocoaPowder(int id, string name, int amount, string instruction = "Use as per recipe")
        : base(id, name, amount, instruction) { }

    public override void Prepare() => Console.WriteLine("Add to other ingredients.");
}

