using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CUE4Parse.Utils;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.ViewModels.Commands;
using FModel.Views.Resources.Converters;

namespace FModel.ViewModels;

public class CommitGroup
{
    public DateTime Date { get; }
    public IReadOnlyList<GitHubCommit> Items { get; }

    public CommitGroup(DateTime date, IReadOnlyList<GitHubCommit> items)
    {
        Date = date;
        Items = items;
    }
}

public partial class UpdateViewModel : ViewModel
{
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;

    private RemindMeCommand _remindMeCommand;
    public RemindMeCommand RemindMeCommand => _remindMeCommand ??= new RemindMeCommand(this);

    public RangeObservableCollection<GitHubCommit> Commits { get; }
    public RangeObservableCollection<CommitGroup> CommitGroups { get; }
    public DataGridCollectionView CommitsView { get; }
    public bool HasNoCommits => CommitGroups.Count == 0;

    public UpdateViewModel()
    {
        Commits = [];
        CommitGroups = [];
        CommitsView = new DataGridCollectionView(Commits)
        {
            // Grouping is rendered through CommitGroups because ItemsControl doesn't honor
            // DataGridCollectionView.GroupDescriptions like WPF did for ListCollectionView.
        };
        Commits.CollectionChanged += (_, _) => RebuildCommitGroups();

        if (UserSettings.Default.NextUpdateCheck < DateTime.Now)
            RemindMeCommand.Execute(this, null);
    }

    public async Task LoadAsync()
    {
        var commits = await _apiEndpointView.GitHubApi.GetCommitHistoryAsync();
        if (commits == null || commits.Length == 0)
            return;

        Commits.AddRange(commits);
        _ = LoadAvatars();

        try
        {
            _ = LoadCoAuthors();
            _ = LoadAssets();
        }
        catch
        {
            //
        }
    }

    private async Task LoadCoAuthors()
    {
        var coAuthorMap = new Dictionary<GitHubCommit, HashSet<string>>();
        foreach (var commit in Commits)
        {
            if (!commit.Commit.Message.Contains("Co-authored-by"))
                continue;

            var regex = GetCoAuthorRegex();
            var matches = regex.Matches(commit.Commit.Message);
            if (matches.Count == 0)
                continue;

            commit.Commit.Message = regex.Replace(commit.Commit.Message, string.Empty).Trim();

            coAuthorMap[commit] = [];
            foreach (Match match in matches)
            {
                if (match.Groups.Count < 3)
                    continue;

                var username = match.Groups[1].Value;
                if (username.Equals("Asval", StringComparison.OrdinalIgnoreCase))
                {
                    username = "4sval"; // found out the hard way co-authored usernames can't be trusted
                }

                coAuthorMap[commit].Add(username);
            }
        }

        if (coAuthorMap.Count == 0)
            return;

        var uniqueUsernames = coAuthorMap.Values.SelectMany(x => x).Distinct().ToArray();
        var authorCache = new Dictionary<string, Author>();
        foreach (var username in uniqueUsernames)
        {
            try
            {
                var author = await _apiEndpointView.GitHubApi.GetUserAsync(username);
                if (author != null)
                    authorCache[username] = author;
            }
            catch
            {
                //
            }
        }

        foreach (var (commit, usernames) in coAuthorMap)
        {
            var coAuthors = usernames
                .Where(username => authorCache.ContainsKey(username))
                .Select(username => authorCache[username])
                .ToArray();

            if (coAuthors.Length > 0)
                commit.CoAuthors = coAuthors;
        }

        await LoadAvatars();
    }

    private async Task LoadAssets()
    {
        var qa = await _apiEndpointView.GitHubApi.GetReleaseAsync("qa");
        var assets = qa.Assets.OrderByDescending(x => x.CreatedAt).ToList();

        for (var i = 0; i < assets.Count; i++)
        {
            var asset = assets[i];
            asset.IsLatest = i == 0;

            var commitSha = asset.Name.SubstringBeforeLast(".zip");
            var commit = Commits.FirstOrDefault(x => x.Sha == commitSha);
            if (commit != null)
            {
                commit.Asset = asset;
            }
            else
            {
                Commits.Add(new GitHubCommit
                {
                    Sha = commitSha,
                    Commit = new Commit
                    {
                        Message = $"FModel ({commitSha[..7]})",
                        Author = new Author { Name = asset.Uploader.Login, Date = asset.CreatedAt }
                    },
                    Author = asset.Uploader,
                    Asset = asset
                });
            }
        }

        await LoadAvatars();
    }

    private void RebuildCommitGroups()
    {
        var groups = Commits
            .OrderByDescending(x => x.Commit?.Author?.Date ?? DateTime.MinValue)
            .GroupBy(x => (x.Commit?.Author?.Date ?? DateTime.MinValue).Date)
            .Select(g => new CommitGroup(g.Key, g.ToList()))
            .ToList();

        CommitGroups.Clear();
        CommitGroups.AddRange(groups);
        RaisePropertyChanged(nameof(HasNoCommits));
    }

    private async Task LoadAvatars()
    {
        var authorsByUrl = Commits
            .SelectMany(x => x.Authors)
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.AvatarUrl))
            .ToArray()
            .GroupBy(x => x.AvatarUrl, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var authors in authorsByUrl)
        {
            if (authors.All(x => x.AvatarImage != null))
                continue;

            var bitmap = await UrlToBitmapConverter.LoadAsync(authors.Key);
            if (bitmap == null)
                continue;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var author in authors)
                    author.AvatarImage = bitmap;
            });
        }
    }

    public void DownloadLatest()
    {
        Commits.FirstOrDefault(x => x.IsDownloadable && x.Asset.IsLatest)?.Download();
    }

    [GeneratedRegex(@"Co-authored-by:\s*(.+?)\s*<(.+?)>", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    private static partial Regex GetCoAuthorRegex();
}
